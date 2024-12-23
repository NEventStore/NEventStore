using System.Diagnostics;
using NEventStore.Persistence;

namespace NEventStore.Diagnostics
{
    // PerformanceCounters are not cross platform

#if NET462

    /// <summary>
    /// Decorate an IPersistStreams implementation with performance counters.
    /// </summary>
    public class PerformanceCounterPersistenceEngine : IPersistStreams
    {
        private readonly PerformanceCounters _counters;
        private readonly IPersistStreams _persistence;

        /// <summary>
        /// Initializes a new instance of the PerformanceCounterPersistenceEngine class.
        /// </summary>
        public PerformanceCounterPersistenceEngine(IPersistStreams persistence, string instanceName)
        {
            _persistence = persistence;
            _counters = new PerformanceCounters(instanceName);
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            _persistence.Initialize();
        }

        /// <inheritdoc/>
        public ICommit Commit(CommitAttempt attempt)
        {
            Stopwatch clock = Stopwatch.StartNew();
            ICommit commit = _persistence.Commit(attempt);
            clock.Stop();
            _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
            return commit;
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            return _persistence.GetFromTo(bucketId, start, end);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            return _persistence.GetFrom(bucketId, start);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            return _persistence.GetFrom(checkpointToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 from, Int64 to)
        {
            return _persistence.GetFromTo(from, to);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            return _persistence.GetFrom(bucketId, checkpointToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 from, Int64 to)
        {
            return _persistence.GetFromTo(bucketId, from, to);
        }

        /// <inheritdoc/>
        public bool AddSnapshot(ISnapshot snapshot)
        {
            bool result = _persistence.AddSnapshot(snapshot);
            if (result)
            {
                _counters.CountSnapshot();
            }

            return result;
        }

        /// <inheritdoc/>
        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _persistence.GetSnapshot(bucketId, streamId, maxRevision);
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _persistence.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        /// <inheritdoc/>
        public virtual void Purge()
        {
            _persistence.Purge();
        }

        /// <inheritdoc/>
        public void Purge(string bucketId)
        {
            _persistence.Purge(bucketId);
        }

        /// <inheritdoc/>
        public void Drop()
        {
            _persistence.Drop();
        }

        /// <inheritdoc/>
        public void DeleteStream(string bucketId, string streamId)
        {
            _persistence.DeleteStream(bucketId, streamId);
        }

        /// <inheritdoc/>
        public bool IsDisposed
        {
            get { return _persistence.IsDisposed; }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the PerformanceCounterPersistenceEngine class.
        /// </summary>
        ~PerformanceCounterPersistenceEngine()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose the performance counter and the wrapped persistence engine.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _counters.Dispose();
            _persistence.Dispose();
        }

        /// <summary>
        /// Unwrap the performance counter and return the wrapped persistence engine.
        /// </summary>
        public IPersistStreams UnwrapPersistenceEngine()
        {
            return _persistence;
        }
    }
#endif
}