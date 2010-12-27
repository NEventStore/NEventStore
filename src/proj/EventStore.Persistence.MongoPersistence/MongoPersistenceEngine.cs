namespace EventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Norm;
    using Norm.BSON;
    using Norm.Configuration;

    public class MongoPersistenceEngine : IPersistStreams
    {
        IMongo store;
        bool disposed;

        public MongoPersistenceEngine(IMongo store)
        {
            this.store = store;
        }

        public void Initialize()
        {
            MongoConfiguration.Initialize(c =>
            {
                c.For<Stream>(stream =>
                    {
                        stream.IdIs(i => i.StreamId);
                    });

            });
        }

        public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
        {
            try
            {
                var collection = this.store.Database.GetCollection<MongoCommit>();
                var results = collection.AsQueryable()
                    .Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();

                return results.Select(mc => mc.ToCommit());
            }
            catch (Exception e)
            {
                throw new PersistenceEngineException(e.Message, e);
            }
        }

        public void Persist(CommitAttempt uncommitted)
        {
            var commit = uncommitted.ToMongoCommit();

            try
            {
                store.Database.GetCollection<MongoCommit>()
                 .Insert(commit);

            }
            catch (MongoException mongoException)
            {
                if (mongoException.Message.StartsWith("E11000"))
                {
                    var committed = store.Database.GetCollection<MongoCommit>()
                        .FindOne(commit);
                        
                    if (committed  != null)
                        throw new DuplicateCommitException();
                    else
                        throw new ConcurrencyException();
                
                }
                throw;
            }

            store.Database.GetCollection<Stream>()
                .Save(new Stream
                          {
                              StreamId = commit.StreamId,
                              HeadRevision = commit.StreamRevision
                          });

        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            var collection = this.store.Database.GetCollection<MongoCommit>();
            var results = collection.AsQueryable()
                .Where(x => !x.Dispatched).ToArray();

            return results.Select(mc => mc.ToCommit());
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
           
            var update = new Expando();
            update["Dispatched"] = M.Set(true);

            store.Database.GetCollection<MongoCommit>()
                .UpdateOne(commit.ToMongoCommit().ToMongoQuery(),update);
        }

        public IEnumerable<StreamToSnapshot> GetStreamsToSnapshot(int maxThreshold)
        {
            var collection = store.Database.GetCollection<Stream>();
            var retval = collection.AsQueryable()
                .Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold).ToArray()
                //todo: fix the name
                .Select(stream => new StreamToSnapshot(stream.StreamId, "", stream.HeadRevision, stream.SnapshotRevision));

            return retval;
        }

        public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
        {
            var commit = new MongoCommit
                                     {
                                         StreamId = streamId,
                                         StreamRevision = streamRevision
                                     }.ToMongoQuery();

            var commitUpdate = new Expando();
            commitUpdate["Snapshot"] = M.Set(snapshot);

            store.Database.GetCollection<MongoCommit>()
                .UpdateOne(commit, commitUpdate);

            var streamUpdate = new Expando();

            streamUpdate["SnapshotRevision"] = M.Set(streamRevision);

            var stream = new Stream { StreamId = streamId };
            store.Database.GetCollection<Stream>()
                .UpdateOne(stream.ToMongoExpando(), streamUpdate);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this.disposed)
                return;

            this.disposed = true;
            this.store.Dispose();
        }


    }
}