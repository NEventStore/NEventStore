namespace EventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;

    public class MongoPersistenceEngine : IPersistStreams
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
        {
            throw new NotImplementedException();
        }

        public void Persist(CommitAttempt uncommitted)
        {
            throw new NotImplementedException();
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