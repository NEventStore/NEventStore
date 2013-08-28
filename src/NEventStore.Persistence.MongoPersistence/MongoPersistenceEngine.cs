namespace NEventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Builders;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class MongoPersistenceEngine : IPersistStreams
    {
        private const string ConcurrencyException = "E1100";
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (MongoPersistenceEngine));
        private readonly MongoCollectionSettings _commitSettings;
        private readonly IDocumentSerializer _serializer;
        private readonly MongoCollectionSettings _snapshotSettings;
        private readonly MongoDatabase _store;
        private readonly MongoCollectionSettings _streamSettings;
        private bool _disposed;
        private int _initialized;

        public MongoPersistenceEngine(MongoDatabase store, IDocumentSerializer serializer)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            _store = store;
            _serializer = serializer;

            _commitSettings = new MongoCollectionSettings {AssignIdOnInsert = false, WriteConcern = WriteConcern.Acknowledged};

            _snapshotSettings = new MongoCollectionSettings {AssignIdOnInsert = false, WriteConcern = WriteConcern.Unacknowledged};

            _streamSettings = new MongoCollectionSettings {AssignIdOnInsert = false, WriteConcern = WriteConcern.Unacknowledged};
        }

        protected virtual MongoCollection<BsonDocument> PersistedCommits
        {
            get { return _store.GetCollection("Commits", _commitSettings); }
        }

        protected virtual MongoCollection<BsonDocument> PersistedStreamHeads
        {
            get { return _store.GetCollection("Streams", _streamSettings); }
        }

        protected virtual MongoCollection<BsonDocument> PersistedSnapshots
        {
            get { return _store.GetCollection("Snapshots", _snapshotSettings); }
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

            TryMongo(() =>
            {
                PersistedCommits.EnsureIndex(IndexKeys.Ascending("Dispatched").Ascending(MongoFields.CommitStamp),
                    IndexOptions.SetName("Dispatched_Index").SetUnique(false));

                PersistedCommits.EnsureIndex(IndexKeys.Ascending("_id.BucketId", "_id.StreamId", "Events.StreamRevision"),
                    IndexOptions.SetName("GetFrom_Index").SetUnique(true));

                PersistedCommits.EnsureIndex(IndexKeys.Ascending(MongoFields.CommitStamp), IndexOptions.SetName("CommitStamp_Index").SetUnique(false));

                PersistedStreamHeads.EnsureIndex(IndexKeys.Ascending("Unsnapshotted"),
                    IndexOptions.SetName("Unsnapshotted_Index").SetUnique(false));
            });
        }

        public virtual IEnumerable<Commit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, bucketId, minRevision, maxRevision);

            return TryMongo(() =>
            {
                IMongoQuery query = Query.And(
                    Query.EQ("_id.BucketId", bucketId),
                    Query.EQ("_id.StreamId", streamId),
                    Query.GTE("Events.StreamRevision", minRevision),
                    Query.LTE("Events.StreamRevision", maxRevision));

                return PersistedCommits
                    .Find(query)
                    .SetSortOrder("Events.StreamRevision")
                    .Select(mc => mc.ToCommit(_serializer));
            });
        }

        public virtual IEnumerable<Commit> GetFrom(string bucketId, DateTime start)
        {
            Logger.Debug(Messages.GettingAllCommitsFrom, start, bucketId);

            return TryMongo(() => PersistedCommits
                .Find(Query.And(Query.EQ("_id.BucketId", bucketId), Query.GTE(MongoFields.CommitStamp, start)))
                .SetSortOrder(MongoFields.Id)
                .Select(x => x.ToCommit(_serializer)));
        }

        public virtual IEnumerable<Commit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            Logger.Debug(Messages.GettingAllCommitsFromTo, start, end, bucketId);

            return TryMongo(() => PersistedCommits
                .Find(Query.And(Query.EQ("_id.BucketId", bucketId), Query.GTE(MongoFields.CommitStamp, start), Query.LT(MongoFields.CommitStamp, end)))
                .SetSortOrder(MongoFields.Id)
                .Select(x => x.ToCommit(_serializer)));
        }

        public virtual void Commit(Commit attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

            TryMongo(() =>
            {
                BsonDocument commit = attempt.ToMongoCommit(_serializer);
                try
                {
                    // for concurrency / duplicate commit detection safe mode is required
                    PersistedCommits.Insert(commit, WriteConcern.Acknowledged);
                    UpdateStreamHeadAsync(attempt.BucketId, attempt.StreamId, attempt.StreamRevision, attempt.Events.Count);
                    Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
                }
                catch (MongoException e)
                {
                    if (!e.Message.Contains(ConcurrencyException))
                    {
                        throw;
                    }
                    Commit savedCommit = PersistedCommits.FindOne(attempt.ToMongoCommitIdQuery()).ToCommit(_serializer);
                    if (savedCommit.CommitId == attempt.CommitId)
                    {
                        throw new DuplicateCommitException();
                    }
                    Logger.Debug(Messages.ConcurrentWriteDetected);
                    throw new ConcurrencyException();
                }
            });
        }

        public virtual IEnumerable<Commit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);

            return TryMongo(() => PersistedCommits
                    .Find(Query.EQ("Dispatched", false))
                    .SetSortOrder(MongoFields.Id)
                    .Select(mc => mc.ToCommit(_serializer)));
        }

        public virtual void MarkCommitAsDispatched(Commit commit)
        {
            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);

            TryMongo(() =>
            {
                IMongoQuery query = commit.ToMongoCommitIdQuery();
                UpdateBuilder update = Update.Set(MongoFields.Dispatched, true);
                PersistedCommits.Update(query, update);
            });
        }

        public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot);

            return TryMongo(() =>
            {
                IMongoQuery query = Query.GTE(MongoFields.Unsnapshotted, maxThreshold);
                return PersistedStreamHeads
                    .Find(query)
                    .SetSortOrder(SortBy.Descending(MongoFields.Unsnapshotted))
                    .Select(x => x.ToStreamHead());
            });
        }

        public virtual Snapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

            return TryMongo(() =>PersistedSnapshots
                .Find(ExtensionMethods.GetSnapshotQuery(bucketId, streamId, maxRevision))
                .SetSortOrder(SortBy.Descending(MongoFields.Id))
                .SetLimit(1)
                .Select(mc => mc.ToSnapshot(_serializer))
                .FirstOrDefault());
        }

        public virtual bool AddSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }
            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.BucketId, snapshot.StreamRevision);
            try
            {
                BsonDocument mongoSnapshot = snapshot.ToMongoSnapshot(_serializer);
                IMongoQuery query = Query.EQ(MongoFields.Id, mongoSnapshot[MongoFields.Id]);
                UpdateBuilder update = Update.Set(MongoFields.Payload, mongoSnapshot[MongoFields.Payload]);

                // Doing an upsert instead of an insert allows us to overwrite an existing snapshot and not get stuck with a
                // stream that needs to be snapshotted because the insert fails and the SnapshotRevision isn't being updated.
                PersistedSnapshots.Update(query, update, UpdateFlags.Upsert);

                // More commits could have been made between us deciding that a snapshot is required and writing it so just
                // resetting the Unsnapshotted count may be a little off. Adding snapshots should be a separate process so
                // this is a good chance to make sure the numbers are still in-sync - it only adds a 'read' after all ...
                BsonDocument streamHeadId = GetStreamHeadId(snapshot.BucketId, snapshot.StreamId);
                StreamHead streamHead = PersistedStreamHeads.FindOneById(streamHeadId).ToStreamHead();
                int unsnapshotted = streamHead.HeadRevision - snapshot.StreamRevision;
                PersistedStreamHeads.Update(
                    Query.EQ(MongoFields.Id, streamHeadId),
                    Update.Set(MongoFields.SnapshotRevision, snapshot.StreamRevision).Set(MongoFields.Unsnapshotted, unsnapshotted));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual void Purge()
        {
            Logger.Warn(Messages.PurgingStorage);
            PersistedCommits.Drop();
            PersistedStreamHeads.Drop();
            PersistedSnapshots.Drop();
        }

        public void Purge(string bucketId)
        {
            throw new NotImplementedException();
        }

        public void Drop()
        {
            Purge();
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            Logger.Debug(Messages.ShuttingDownPersistence);
            _disposed = true;
        }

        private void UpdateStreamHeadAsync(string bucketId, string streamId, int streamRevision, int eventsCount)
        {
            ThreadPool.QueueUserWorkItem(x => 
                TryMongo(() =>
                {
                    BsonDocument streamHeadId = GetStreamHeadId(bucketId, streamId);
                    PersistedStreamHeads.Update(
                        Query.EQ(MongoFields.Id, streamHeadId),
                        Update
                            .Set("HeadRevision", streamRevision)
                            .Inc("SnapshotRevision", 0)
                            .Inc("Unsnapshotted", eventsCount),
                        UpdateFlags.Upsert);
                }), null);
        }

        protected virtual T TryMongo<T>(Func<T> callback)
        {
            T results = default(T);
            TryMongo(() => { results = callback(); });
            return results;
        }

        protected virtual void TryMongo(Action callback)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Attempt to use storage after it has been disposed.");
            }
            try
            {
                callback();
            }
            catch (MongoConnectionException e)
            {
                Logger.Warn(Messages.StorageUnavailable);
                throw new StorageUnavailableException(e.Message, e);
            }
            catch (MongoException e)
            {
                Logger.Error(Messages.StorageThrewException, e.GetType());
                throw new StorageException(e.Message, e);
            }
        }

        private static BsonDocument GetStreamHeadId(string bucketId, string streamId)
        {
            var id = new BsonDocument();
            id[MongoFields.BucketId] = bucketId;
            id[MongoFields.StreamId] = streamId;
            return id;
        }
    }
}