namespace NEventStore.Persistence.RavenPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading;
    using System.Transactions;
    using NEventStore.Logging;
    using NEventStore.Persistence.RavenPersistence.Indexes;
    using NEventStore.Serialization;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Exceptions;
    using Raven.Client.Indexes;
    using Raven.Json.Linq;
    using ConcurrencyException = NEventStore.ConcurrencyException;

    public class RavenPersistenceEngine : IPersistStreams
    {
        private const int MinPageSize = 10;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (RavenPersistenceEngine));
        private readonly bool _consistentQueries;
        private readonly int _pageSize;
        private readonly TransactionScopeOption _scopeOption;
        private readonly IDocumentSerializer _serializer;
        private readonly IDocumentStore _store;
        private int _initialized;

        public RavenPersistenceEngine(IDocumentStore store, RavenConfiguration config)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (config.Serializer == null)
            {
                throw new ArgumentException(Messages.SerializerCannotBeNull, "config");
            }

            if (config.PageSize < MinPageSize)
            {
                throw new ArgumentException(Messages.PagingSizeTooSmall, "config");
            }

            _store = store;
            _serializer = config.Serializer;
            _scopeOption = config.ScopeOption;
            _consistentQueries = config.ConsistentQueries;
            _pageSize = config.PageSize;
        }

        public IDocumentStore Store
        {
            get { return _store; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Initialize()
        {
            if (Interlocked.Increment(ref _initialized) > 1)
            {
                return;
            }

            Logger.Debug(Messages.InitializingStorage);

            TryRaven(() =>
            {
                using (TransactionScope scope = OpenCommandScope())
                {
                    new RavenCommitByDate().Execute(_store);
                    new RavenCommitByRevisionRange().Execute(_store);
                    new RavenCommitsByDispatched().Execute(_store);
                    new RavenSnapshotByStreamIdAndRevision().Execute(_store);
                    new RavenStreamHeadBySnapshotAge().Execute(_store);
                    new EventStoreDocumentsByEntityName().Execute(_store);
                    scope.Complete();
                }

                return true;
            });
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, bucketId, minRevision, maxRevision);

            return
                QueryCommits<RavenCommitByRevisionRange>(x => 
                    x.BucketId == bucketId &&
                    x.StreamId == streamId &&
                    x.StreamRevision >= minRevision &&
                    x.StartingStreamRevision <= maxRevision)
                    .OrderBy(x => x.CommitSequence);
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            Logger.Debug(Messages.GettingAllCommitsFrom, start, bucketId);

            return QueryCommits<RavenCommitByDate>(x => x.BucketId == bucketId && x.CommitStamp >= start).OrderBy(x => x.CommitStamp);
        }

        public virtual IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            Logger.Debug(Messages.GettingAllCommitsFromTo, start, end, bucketId);

            return QueryCommits<RavenCommitByDate>(x => x.BucketId == bucketId && x.CommitStamp >= start && x.CommitStamp < end).OrderBy(x => x.CommitStamp);
        }

        public virtual void Commit(ICommit attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence, attempt.BucketId);

            try
            {
                TryRaven(() =>
                {
                    using (TransactionScope scope = OpenCommandScope())
                    using (IDocumentSession session = _store.OpenSession())
                    {
                        session.Advanced.UseOptimisticConcurrency = true;
                        var doc = attempt.ToRavenCommit(_serializer);
                        session.Store(doc);
                        session.SaveChanges();
                        scope.Complete();
                    }

                    Logger.Debug(Messages.CommitPersisted, attempt.CommitId, attempt.BucketId);
                    SaveStreamHead(attempt.ToRavenStreamHead());
                    return true;
                });
            }
            catch (Raven.Abstractions.Exceptions.ConcurrencyException)
            {
                RavenCommit savedCommit = LoadSavedCommit(attempt);
                if (savedCommit.CommitId == attempt.CommitId)
                {
                    throw new DuplicateCommitException();
                }

                Logger.Debug(Messages.ConcurrentWriteDetected);
                throw new ConcurrencyException();
            }
        }

        public virtual IEnumerable<ICommit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);
            return QueryCommits<RavenCommitsByDispatched>(c => c.Dispatched == false).OrderBy(x => x.CommitSequence);
        }

        public virtual void MarkCommitAsDispatched(ICommit commit)
        {
            if (commit == null)
            {
                throw new ArgumentNullException("commit");
            }

            var patch = new PatchRequest {Type = PatchCommandType.Set, Name = "Dispatched", Value = RavenJToken.Parse("true")};
            var data = new PatchCommandData {Key = commit.ToRavenCommitId(), Patches = new[] {patch}};

            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId, commit.BucketId);

            TryRaven(() =>
            {
                using (TransactionScope scope = OpenCommandScope())
                using (IDocumentSession session = _store.OpenSession())
                {
                    session.Advanced.DocumentStore.DatabaseCommands.Batch(new[] {data});
                    session.SaveChanges();
                    scope.Complete();
                    return true;
                }
            });
        }

        public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot, bucketId);

            return
                Query<RavenStreamHead, RavenStreamHeadBySnapshotAge>(s => s.BucketId == bucketId && s.SnapshotAge >= maxThreshold)
                    .Select(s => s.ToStreamHead());
        }

        public virtual ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

            return
                Query<RavenSnapshot, RavenSnapshotByStreamIdAndRevision>(x =>  
                    x.BucketId == bucketId &&
                    x.StreamId == streamId &&
                    x.StreamRevision <= maxRevision)
                    .OrderByDescending(x => x.StreamRevision)
                    .FirstOrDefault()
                    .ToSnapshot(_serializer);
        }

        public virtual bool AddSnapshot(ISnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision, snapshot.BucketId);

            try
            {
                return TryRaven(() =>
                {
                    using (TransactionScope scope = OpenCommandScope())
                    using (IDocumentSession session = _store.OpenSession())
                    {
                        RavenSnapshot ravenSnapshot = snapshot.ToRavenSnapshot(_serializer);
                        session.Store(ravenSnapshot);
                        session.SaveChanges();
                        scope.Complete();
                    }

                    SaveStreamHead(snapshot.ToRavenStreamHead());

                    return true;
                });
            }
            catch (Raven.Abstractions.Exceptions.ConcurrencyException)
            {
                return false;
            }
        }

        public virtual void Purge()
        {
            Logger.Warn(Messages.PurgingStorage);

            TryRaven(() =>
            {
                using (TransactionScope scope = OpenCommandScope())
                using (IDocumentSession session = _store.OpenSession())
                {
                    PurgeDocuments(session);

                    session.SaveChanges();
                    scope.Complete();
                    return true;
                }
            });
        }

        public void Purge(string bucketId)
        {
            throw new NotImplementedException();
        }

        public void Drop()
        {
            Purge();
        }

        public void DeleteStream(string bucketId, string streamId)
        {
            throw new NotImplementedException("Engine to be rewritten");
        }

        public IEnumerable<ICommit> GetFrom(int checkpoint)
        {
            throw new NotImplementedException("Engine to be rewritten");
        }

        public bool IsDisposed
        {
            get { return _store.WasDisposed; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Logger.Debug(Messages.ShuttingDownPersistence);
            _store.Dispose();
        }

        private RavenCommit LoadSavedCommit(ICommit attempt)
        {
            Logger.Debug(Messages.DetectingConcurrency);

            return TryRaven(() =>
            {
                using (TransactionScope scope = OpenQueryScope())
                using (IDocumentSession session = _store.OpenSession())
                {
                    var commit = session.Load<RavenCommit>(attempt.ToRavenCommitId());
                    scope.Complete();
                    return commit;
                }
            });
        }

        private void PurgeDocuments(IDocumentSession session)
        {
            Func<Type, string> getTagCondition = t => "Tag:" + session.Advanced.DocumentStore.Conventions.GetTypeTagName(t);

            string queryText = "(" + getTagCondition(typeof (RavenCommit)) + " OR " + getTagCondition(typeof (RavenSnapshot)) + " OR " +
                getTagCondition(typeof (RavenStreamHead)) + ")";

            var query = new IndexQuery {Query = queryText};

            const string index = "EventStoreDocumentsByEntityName";

            while (HasDocs(index, query))
            {
                session.Advanced.DocumentStore.DatabaseCommands.DeleteByIndex(index, query, true);
            }
        }

        private bool HasDocs(string index, IndexQuery query)
        {
            while (_store.DatabaseCommands.GetStatistics().StaleIndexes.Contains(index))
            {
                Thread.Sleep(50);
            }

            return _store.DatabaseCommands.Query(index, query, null, true).TotalResults != 0;
        }

        private IEnumerable<Commit> QueryCommits<TIndex>(Expression<Func<RavenCommit, bool>> query)
            where TIndex : AbstractIndexCreationTask, new()
        {
            IEnumerable<RavenCommit> commits = Query<RavenCommit, TIndex>(query);

            return commits.Select(x => x.ToCommit(_serializer));
        }

        private IEnumerable<T> Query<T, TIndex>(params Expression<Func<T, bool>>[] conditions)
            where TIndex : AbstractIndexCreationTask, new()
        {
            return new ResetableEnumerable<T>(() => PagedQuery<T, TIndex>(conditions));
        }

        private IEnumerable<T> PagedQuery<T, TIndex>(Expression<Func<T, bool>>[] conditions) where TIndex : AbstractIndexCreationTask, new()
        {
            int total = 0;
            RavenQueryStatistics stats;

            do
            {
                using (IDocumentSession session = _store.OpenSession())
                {
                    int requestsForSession = 0;

                    do
                    {
                        T[] docs = PerformQuery<T, TIndex>(session, conditions, total, _pageSize, out stats);
                        total += docs.Length;
                        requestsForSession++;

                        foreach (var d in docs)
                        {
                            yield return d;
                        }
                    } while (total < stats.TotalResults && requestsForSession < session.Advanced.MaxNumberOfRequestsPerSession);
                }
            } while (total < stats.TotalResults);
        }

        private T[] PerformQuery<T, TIndex>(
            IDocumentSession session, IEnumerable<Expression<Func<T, bool>>> conditions, int skip, int take, out RavenQueryStatistics stats)
            where TIndex : AbstractIndexCreationTask, new()
        {
            try
            {
                using (TransactionScope scope = OpenQueryScope())
                {
                    IQueryable<T> query = session.Query<T, TIndex>().Customize(x =>
                    {
                        if (_consistentQueries)
                        {
                            x.WaitForNonStaleResults();
                        }
                    }).Statistics(out stats);

                    query = conditions.Aggregate(query, (current, condition) => current.Where(condition));

                    var results = query
                        .Skip(skip).Take(take)
                        .ToArray();

                    scope.Complete();

                    return results;
                }
            }
            catch (WebException e)
            {
                Logger.Warn(Messages.StorageUnavailable);
                throw new StorageUnavailableException(e.Message, e);
            }
            catch (ObjectDisposedException)
            {
                Logger.Warn(Messages.StorageAlreadyDisposed);
                throw;
            }
            catch (Exception e)
            {
                Logger.Error(Messages.StorageThrewException, e.GetType());
                throw new StorageException(e.Message, e);
            }
        }

        private void SaveStreamHead(RavenStreamHead streamHead)
        {
            if (_consistentQueries)
            {
                SaveStreamHeadAsync(streamHead);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(x => SaveStreamHeadAsync(streamHead), null);
            }
        }

        private void SaveStreamHeadAsync(RavenStreamHead updated)
        {
            TryRaven(() =>
            {
                using (TransactionScope scope = OpenCommandScope())
                using (IDocumentSession session = _store.OpenSession())
                {
                    RavenStreamHead current = session.Load<RavenStreamHead>(RavenStreamHead.GetStreamHeadId(updated.BucketId, updated.StreamId)) ?? updated;
                    current.HeadRevision = updated.HeadRevision;

                    if (updated.SnapshotRevision > 0)
                    {
                        current.SnapshotRevision = updated.SnapshotRevision;
                    }

                    session.Advanced.UseOptimisticConcurrency = false;
                    session.Store(current);
                    session.SaveChanges();
                    scope.Complete(); // if this fails it's no big deal, stream heads can be updated whenever
                }
                return true;
            });
        }

        protected virtual T TryRaven<T>(Func<T> callback)
        {
            try
            {
                return callback();
            }
            catch (WebException e)
            {
                Logger.Warn(Messages.StorageUnavailable);
                throw new StorageUnavailableException(e.Message, e);
            }
            catch (NonUniqueObjectException e)
            {
                Logger.Warn(Messages.DuplicateCommitDetected);
                throw new DuplicateCommitException(e.Message, e);
            }
            catch (Raven.Abstractions.Exceptions.ConcurrencyException)
            {
                Logger.Warn(Messages.ConcurrentWriteDetected);
                throw;
            }
            catch (ObjectDisposedException)
            {
                Logger.Warn(Messages.StorageAlreadyDisposed);
                throw;
            }
            catch (Exception e)
            {
                Logger.Error(Messages.StorageThrewException, e.GetType());
                throw new StorageException(e.Message, e);
            }
        }

        protected virtual TransactionScope OpenQueryScope()
        {
            return OpenCommandScope() ?? new TransactionScope(TransactionScopeOption.Suppress);
        }

        protected virtual TransactionScope OpenCommandScope()
        {
            return new TransactionScope(_scopeOption);
        }
    }
}
