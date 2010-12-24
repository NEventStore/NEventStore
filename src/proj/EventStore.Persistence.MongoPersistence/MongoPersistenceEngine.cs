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

        public MongoPersistenceEngine(IMongo store)
        {
            this.store = store;
        }

        public void Initialize()
        {
            MongoConfiguration.Initialize(c =>
            {
                c.For<Commit>(commit=>
                    {
                        //todo: check with jonathan if this will fly
                        commit.IdIs(i=>i.CommitId);
                    });
                
            });
        }

        public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                try
                {
                    var collection = this.store.Database.GetCollection<Commit>();
                    return collection.AsQueryable()
                        .Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();
                }
                catch (Exception e)
                {
                    throw new PersistenceEngineException(e.Message, e);
                }
            }
        }

        public void Persist(CommitAttempt uncommitted)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                try
                {
                    var collection = this.store.Database.GetCollection<Commit>();
                    collection.Save(uncommitted.ToCommit());

                    //todo: detect concurrency confilcts
                }
                catch (Exception e)
                {
                    throw new PersistenceEngineException(e.Message, e);
                }
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
    }
}