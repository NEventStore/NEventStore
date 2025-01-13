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
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        /// <inheritdoc/>
        public ICommit? Commit(CommitAttempt attempt)
        {
            Stopwatch clock = Stopwatch.StartNew();
            var commit = _persistence.Commit(attempt);
            clock.Stop();
            _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
            return commit;
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            return _persistence.GetFromAsync(bucketId, streamId, minRevision, maxRevision, observer, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            Stopwatch clock = Stopwatch.StartNew();
            var commit = await _persistence.CommitAsync(attempt, cancellationToken).ConfigureAwait(false);
            clock.Stop();
            _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
            return commit;
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate)
        {
            return _persistence.GetFromTo(bucketId, startDate, endDate);
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFrom(Int64 checkpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime startDate)
        {
            return _persistence.GetFrom(bucketId, startDate);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            return _persistence.GetFrom(checkpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromAsync(Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return _persistence.GetFromAsync(checkpointToken, asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return _persistence.GetFromTo(fromCheckpointToken, toCheckpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(Int64 fromCheckpointToken, Int64 toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return _persistence.GetFromToAsync(fromCheckpointToken, toCheckpointToken, asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            return _persistence.GetFrom(bucketId, checkpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return _persistence.GetFromAsync(bucketId, checkpointToken, asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return _persistence.GetFromTo(bucketId, fromCheckpointToken, toCheckpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(string bucketId, long fromCheckpointToken, long toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return _persistence.GetFromToAsync(bucketId, fromCheckpointToken, toCheckpointToken, asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public ISnapshot? GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _persistence.GetSnapshot(bucketId, streamId, maxRevision);
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
        public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _persistence.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        /// <inheritdoc/>
        public Task<ISnapshot?> GetSnapshotAsync(string bucketId, string streamId, int maxRevision, CancellationToken cancellationToken)
        {
            return _persistence.GetSnapshotAsync(bucketId, streamId, maxRevision, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> AddSnapshotAsync(ISnapshot snapshot, CancellationToken cancellationToken)
        {
            bool result = await _persistence.AddSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            if (result)
            {
                _counters.CountSnapshot();
            }

            return result;
        }

        /// <inheritdoc/>
        public Task GetStreamsToSnapshotAsync(string bucketId, int maxThreshold, IAsyncObserver<IStreamHead> asyncObserver, CancellationToken cancellationToken)
        {
            return _persistence.GetStreamsToSnapshotAsync(bucketId, maxThreshold, asyncObserver, cancellationToken);
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
        public Task PurgeAsync(CancellationToken cancellationToken)
        {
            return _persistence.PurgeAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task PurgeAsync(string bucketId, CancellationToken cancellationToken)
        {
            return _persistence.PurgeAsync(bucketId, cancellationToken);
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
        public Task DeleteStreamAsync(string bucketId, string streamId, CancellationToken cancellationToken)
        {
            return _persistence.DeleteStreamAsync(bucketId, streamId, cancellationToken);
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