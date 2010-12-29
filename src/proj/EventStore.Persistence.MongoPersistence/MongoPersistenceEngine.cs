namespace EventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Norm;
    using Norm.BSON;
    using Norm.Configuration;
    using Norm.Protocol.Messages;
    using Serialization;

    public class MongoPersistenceEngine : IPersistStreams
    {
        readonly IMongo store;
        readonly ISerialize serializer;
        bool disposed;

        public MongoPersistenceEngine(IMongo store, ISerialize serializer)
        {
            this.store = store;
            this.serializer = serializer;
        }

        public void Initialize()
        {
            MongoConfiguration.Initialize(c =>
            {
                c.For<Stream>(stream => stream.IdIs(i => i.StreamId)); 
                
            });
        
            store.Database.GetCollection<MongoCommit>()
                .CreateIndex(mc => mc.Dispatched,"Dispatched_Index",false, IndexOption.Ascending);
  
            store.Database.GetCollection<MongoCommit>()
                .CreateIndex(mc => new{mc.StreamId,mc.StreamRevision}, "GetFrom_Index", false, IndexOption.Ascending);
        }

        public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
        {
            var collection = this.store.Database.GetCollection<MongoCommit>();
            var snapshotCommit = collection.AsQueryable()
                .Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Snapshot != null)
                .OrderByDescending(o=>o.StreamRevision)
                .Take(1)
                .FirstOrDefault();

            long snapshotRevision = 0;
            
            if(snapshotCommit!= null)
                snapshotRevision= snapshotCommit.StreamRevision;

            var results = collection.AsQueryable()
                .Where(x => x.StreamId == streamId && x.StreamRevision >= snapshotRevision && x.StreamRevision <= maxRevision)
                .ToArray();

            return results.Select(mc => mc.ToCommit(serializer));
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
        {
            try
            {
                var collection = this.store.Database.GetCollection<MongoCommit>();
                var results = collection.AsQueryable()
                    .Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();

                return results.Select(mc => mc.ToCommit(serializer));
            }
            catch (Exception e)
            {
                throw new PersistenceEngineException(e.Message, e);
            }
        }

        public void Persist(CommitAttempt uncommitted)
        {
            var commit = uncommitted.ToMongoCommit(serializer);

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

            return results.Select(mc => mc.ToCommit(serializer));
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            store.Database.GetCollection<MongoCommit>()
                .Update(commit.ToMongoCommit(serializer).ToMongoExpando(), u => u.SetValue(mc => mc.Dispatched, true));
        }

        public IEnumerable<StreamToSnapshot> GetStreamsToSnapshot(int maxThreshold)
        {
            var collection = store.Database.GetCollection<Stream>();
            var retval = collection.AsQueryable()
                .Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold).ToArray()
                .Select(stream => new StreamToSnapshot(stream.StreamId, stream.Name, stream.HeadRevision, stream.SnapshotRevision));

            return retval;
        }

        public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
        {
           var commit = new Expando();

            commit["StreamId"] = streamId;
            commit["StreamRevision"] = streamRevision;

            store.Database.GetCollection<MongoCommit>()
                .Update(commit, u=>u.SetValue(mc=>mc.Snapshot, serializer.Serialize(snapshot)));

            var stream = new Stream { StreamId = streamId };
            store.Database.GetCollection<Stream>()
                .Update(stream.ToMongoExpando(), u=>u.SetValue(s=>s.SnapshotRevision,streamRevision));
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