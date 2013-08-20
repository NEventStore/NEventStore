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

        public virtual void Commit(Commit attempt)
        {
            Stopwatch clock = Stopwatch.StartNew();
            _persistence.Commit(attempt);
            clock.Stop();

            _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
        }

        public virtual void MarkCommitAsDispatched(Commit commit)
        {
            _persistence.MarkCommitAsDispatched(commit);
            _counters.CountCommitDispatched();
        }

        public IEnumerable<Commit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            return _persistence.GetFromTo(bucketId, start, end);
        }

        public virtual IEnumerable<Commit> GetUndispatchedCommits()
        {
            return _persistence.GetUndispatchedCommits();
        }

        public virtual IEnumerable<Commit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        public virtual IEnumerable<Commit> GetFrom(string bucketId, DateTime start)
        {
            return _persistence.GetFrom(bucketId, start);
        }

        public virtual bool AddSnapshot(Snapshot snapshot)
        {
            bool result = _persistence.AddSnapshot(snapshot);
            if (result)
            {
                _counters.CountSnapshot();
            }

            return result;
        }

        public virtual Snapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _persistence.GetSnapshot(bucketId, streamId, maxRevision);
        }

        public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _persistence.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        public virtual void Purge()
        {
            _persistence.Purge();
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