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

        public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            return _persistence.GetFromTo(start, end);
        }

        public virtual IEnumerable<Commit> GetUndispatchedCommits()
        {
            return _persistence.GetUndispatchedCommits();
        }

        public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(streamId, minRevision, maxRevision);
        }

        public virtual IEnumerable<Commit> GetFrom(DateTime start)
        {
            return _persistence.GetFrom(start);
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

        public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            return _persistence.GetSnapshot(streamId, maxRevision);
        }

        public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            return _persistence.GetStreamsToSnapshot(maxThreshold);
        }

        public virtual void Purge()
        {
            _persistence.Purge();
        }

        public void Drop()
        {
            _persistence.Drop();
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
