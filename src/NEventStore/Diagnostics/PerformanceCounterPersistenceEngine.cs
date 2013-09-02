namespace NEventStore.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NEventStore.Persistence;

    public class PerformanceCounterPersistenceEngine : IPersistStreams
    {
        private readonly PerformanceCounters _counters;
        private readonly IPersistStreams _persistence;

        public PerformanceCounterPersistenceEngine(IPersistStreams persistence, string instanceName)
        {
            _persistence = persistence;
            _counters = new PerformanceCounters(instanceName);
        }

        public virtual void Initialize()
        {
            _persistence.Initialize();
        }

        public virtual void Commit(ICommit attempt)
        {
            Stopwatch clock = Stopwatch.StartNew();
            _persistence.Commit(attempt);
            clock.Stop();

            _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
        }

        public virtual void MarkCommitAsDispatched(ICommit commit)
        {
            _persistence.MarkCommitAsDispatched(commit);
            _counters.CountCommitDispatched();
        }

        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            return _persistence.GetFromTo(bucketId, start, end);
        }

        public virtual IEnumerable<ICommit> GetUndispatchedCommits()
        {
            return _persistence.GetUndispatchedCommits();
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            return _persistence.GetFrom(bucketId, start);
        }

        public virtual bool AddSnapshot(ISnapshot snapshot)
        {
            bool result = _persistence.AddSnapshot(snapshot);
            if (result)
            {
                _counters.CountSnapshot();
            }

            return result;
        }

        public virtual ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _persistence.GetSnapshot(bucketId, streamId, maxRevision);
        }

        public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _persistence.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        public virtual void Purge()
        {
            _persistence.Purge();
        }

        public void Purge(string bucketId)
        {
            _persistence.Purge(bucketId);
        }

        public void Drop()
        {
            _persistence.Drop();
        }

        public void DeleteStream(string bucketId, string streamId)
        {
            _persistence.DeleteStream(bucketId, streamId);
        }

        public IEnumerable<ICommit> GetFrom(int checkpoint)
        {
            return _persistence.GetFrom(checkpoint);
        }

        public bool IsDisposed
        {
            get { return _persistence.IsDisposed; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PerformanceCounterPersistenceEngine()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _counters.Dispose();
            _persistence.Dispose();
        }
    }
}