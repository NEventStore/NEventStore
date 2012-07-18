namespace EventStore.Persistence.RavenPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading;
    using System.Transactions;
    using Indexes;
    using Logging;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Connection;
    using Raven.Client.Exceptions;
    using Raven.Client.Indexes;
    using Raven.Json.Linq;
    using Serialization;

    public class RavenPersistenceEngine : IPersistStreams
    {
        private const int MinPageSize = 10;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(RavenPersistenceEngine));
        private readonly IDocumentStore store;
        private readonly IDocumentSerializer serializer;
        private readonly TransactionScopeOption scopeOption;
        private readonly bool consistentQueries;
        private readonly int pageSize;
        private int initialized;
        private readonly string partition;
        
        public RavenPersistenceEngine(IDocumentStore store, RavenConfiguration config)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            if (config == null)
                throw new ArgumentNullException("config");

            if (config.Serializer == null)
                throw new ArgumentException(Messages.SerializerCannotBeNull, "config");

            if (config.PageSize < MinPageSize)
                throw new ArgumentException(Messages.PagingSizeTooSmall, "config");

            this.store = store;
            this.serializer = config.Serializer;
            this.scopeOption = config.ScopeOption;
            this.consistentQueries = config.ConsistentQueries;
            this.pageSize = config.PageSize;
            this.partition = config.Partition;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Logger.Debug(Messages.ShuttingDownPersistence);
            this.store.Dispose();
        }

        public virtual void Initialize()
        {
            if (Interlocked.Increment(ref this.initialized) > 1)
                return;

            Logger.Debug(Messages.InitializingStorage);

            this.TryRaven(() =>
            {
                using (var scope = this.OpenCommandScope())
                {
                    new RavenCommitByDate().Execute(this.store);
                    new RavenCommitByRevisionRange().Execute(this.store);
                    new RavenCommitsByDispatched().Execute(this.store);
                    new RavenSnapshotByStreamIdAndRevision().Execute(this.store);
                    new RavenStreamHeadBySnapshotAge().Execute(this.store);
                    new EventStoreDocumentsByEntityName().Execute(this.store);
                    scope.Complete();
                }

                return true;
            });
        }

        public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);

            return this.QueryCommits<RavenCommitByRevisionRange>(x =>
                    x.StreamId == streamId && x.StreamRevision >= minRevision && x.StartingStreamRevision <= maxRevision)
                .OrderBy(x => x.CommitSequence);
        }

        public virtual IEnumerable<Commit> GetFrom(DateTime start)
        {
            Logger.Debug(Messages.GettingAllCommitsFrom, start);

            return this.QueryCommits<RavenCommitByDate>(x => x.CommitStamp >= start)
                .OrderBy(x => x.CommitStamp);
        }

        public virtual void Commit(Commit attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit,
                attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

            try
            {
                this.TryRaven(() =>
                {
                    using (var scope = this.OpenCommandScope())
                    using (var session = this.store.OpenSession())
                    {
                        session.Advanced.UseOptimisticConcurrency = true;
                        session.Store(attempt.ToRavenCommit(this.partition, this.serializer));
                        session.SaveChanges();
                        scope.Complete();
                    }

                    Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
                    this.SaveStreamHead(attempt.ToRavenStreamHead(this.partition));
                    return true;
                });
            }
            catch (Raven.Abstractions.Exceptions.ConcurrencyException)
            {
                var savedCommit = this.LoadSavedCommit(attempt);
                if (savedCommit.CommitId == attempt.CommitId)
                    throw new DuplicateCommitException();

                Logger.Debug(Messages.ConcurrentWriteDetected);
                throw new ConcurrencyException();
            }
        }

        private RavenCommit LoadSavedCommit(Commit attempt)
        {
            Logger.Debug(Messages.DetectingConcurrency);

            return this.TryRaven(() =>
            {
                using (var scope = this.OpenQueryScope())
                using (var session = this.store.OpenSession())
                {
                    var commit = session.Load<RavenCommit>(attempt.ToRavenCommitId(this.partition));
                    scope.Complete();
                    return commit;
                }
            });
        }

        public virtual IEnumerable<Commit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);
            return this.QueryCommits<RavenCommitsByDispatched>(c => c.Dispatched == false)
                .OrderBy(x => x.CommitStamp);
        }

        public virtual void MarkCommitAsDispatched(Commit commit)
        {
            if (commit == null)
                throw new ArgumentNullException("commit");

            var patch = new PatchRequest
            {
                Type = PatchCommandType.Set,
                Name = "Dispatched",
                Value = RavenJToken.Parse("true")
            };
            var data = new PatchCommandData
            {
                Key = commit.ToRavenCommitId(this.partition),
                Patches = new[] { patch }
            };

            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);

            this.TryRaven(() =>
            {
                using (var scope = this.OpenCommandScope())
                using (var session = this.store.OpenSession())
                {
                    session.Advanced.DatabaseCommands.Batch(new[] { data });
                    session.SaveChanges();
                    scope.Complete();
                    return true;
                }
            });
        }

        public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot);

            return this.Query<RavenStreamHead, RavenStreamHeadBySnapshotAge>(s => s.SnapshotAge >= maxThreshold && s.Partition == this.partition)
                .Select(s => s.ToStreamHead());
        }

        public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

            return Query<RavenSnapshot, RavenSnapshotByStreamIdAndRevision>(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Partition == this.partition)
                .OrderByDescending(x => x.StreamRevision)
                .FirstOrDefault()
                .ToSnapshot(this.serializer);
        }

        public virtual bool AddSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
                return false;

            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

            try
            {
                return this.TryRaven(() =>
                {
                    using (var scope = this.OpenCommandScope())
                    using (var session = this.store.OpenSession())
                    {
                        var ravenSnapshot = snapshot.ToRavenSnapshot(this.partition, this.serializer);
                        session.Store(ravenSnapshot);
                        session.SaveChanges();
                        scope.Complete();
                    }

                    this.SaveStreamHead(snapshot.ToRavenStreamHead(this.partition));

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

			this.TryRaven(() =>
			{
				using (var scope = this.OpenCommandScope())
				using (var session = this.store.OpenSession())
				{
                    PurgeDocuments(session);

					session.SaveChanges();
					scope.Complete();
					return true;
				}
			});
		}

        private void PurgeDocuments(IDocumentSession session)
        {
            Func<Type, string> getTagCondition = t => "Tag:" + session.Advanced.DocumentStore.Conventions.GetTypeTagName(t);

            var typeQuery = "(" + getTagCondition(typeof(RavenCommit)) + " OR " + getTagCondition(typeof(RavenSnapshot)) + " OR " + getTagCondition(typeof(RavenStreamHead)) + ")";
            var partitionQuery = "Partition:" + (this.partition ?? "[[NULL_VALUE]]");
            var queryText = partitionQuery + " AND " + typeQuery;

            var query = new IndexQuery { Query = queryText  };

            session.Advanced.DatabaseCommands
                .DeleteByIndex("EventStoreDocumentsByEntityName", query, true);
        }

        private IEnumerable<Commit> QueryCommits<TIndex>(Expression<Func<RavenCommit, bool>> query)
            where TIndex : AbstractIndexCreationTask, new()
        {
            var commits = Query<RavenCommit, TIndex>(query, c => c.Partition == this.partition);

            return commits.Select(x => x.ToCommit(this.serializer));
        }

        private IEnumerable<T> Query<T, TIndex>(params Expression<Func<T, bool>>[] conditions)
            where TIndex : AbstractIndexCreationTask, new()
        {
            return this.TryRaven(() =>
            {
                var scope = this.OpenQueryScope();

                try
                {
                    using (var session = this.OpenQuerySession())
                    {
                        IQueryable<T> query = session.Query<T, TIndex>()
                            .Customize(x => { if (this.consistentQueries) x.WaitForNonStaleResults(); });

                        query = conditions.Aggregate(query, (current, condition) => current.Where(condition));

                        return query.Page(this.pageSize, scope);
                    }
                }
                catch (Exception)
                {
                    scope.Dispose();
                    throw;
                }
            });
        }
        private IDocumentSession OpenQuerySession()
        {
            var session = this.store.OpenSession();

            // defaults to 30 total requests per session (not good for paging over large data sets)
            // which may be encountered when calling GetFrom() and enumerating over the entire store.
            // see http://ravendb.net/documentation/safe-by-default for more information.
            session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

            return session;
        }

        private void SaveStreamHead(RavenStreamHead streamHead)
        {
            if (this.consistentQueries)
                this.SaveStreamHeadAsync(streamHead);
            else
                ThreadPool.QueueUserWorkItem(x => this.SaveStreamHeadAsync(streamHead), null);
        }
        private void SaveStreamHeadAsync(RavenStreamHead updated)
        {
            this.TryRaven(() =>
            {
                using (var scope = this.OpenCommandScope())
                using (var session = this.store.OpenSession())
                {
                    var current = session.Load<RavenStreamHead>(updated.StreamId.ToRavenStreamId(this.partition)) ?? updated;
                    current.HeadRevision = updated.HeadRevision;

                    if (updated.SnapshotRevision > 0)
                        current.SnapshotRevision = updated.SnapshotRevision;

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
            return this.OpenCommandScope() ?? new TransactionScope(TransactionScopeOption.Suppress);
        }
        protected virtual TransactionScope OpenCommandScope()
        {
            return new TransactionScope(this.scopeOption);
        }
    }
}