namespace EventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using Norm;
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
                c.For<MongoCommit>(commit =>
                    {
                        //Id is the default convention
                //        commit.IdIs(i => i.Id);
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
                var results= collection.AsQueryable()
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
                var collection = this.store.Database.GetCollection<MongoCommit>();
                collection.Save(commit);

                //todo: detect concurrency confilcts
            }
            catch (Exception e)
            {
                throw new PersistenceEngineException(e.Message, e);
            }
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            throw new NotImplementedException();
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<StreamToSnapshot> GetStreamsToSnapshot(int maxThreshold)
        {
            throw new NotImplementedException();
        }

        public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
        {
            throw new NotImplementedException();
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

    public class MongoCommit
    {
        private const string IdFormat = "{0}.{1}";

        public string Id
        {
            get { return IdFormat.FormatWith(this.StreamId, this.CommitSequence); }
        }
        public Guid StreamId { get; set; }
        public Guid CommitId { get; set; }
        public long StreamRevision { get; set; }
        public long CommitSequence { get; set; }
        public Dictionary<string, object> Headers { get; set; }
        public List<EventMessage> Events { get; set; }
        public object Snapshot { get; set; }
    }
}