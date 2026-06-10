using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Persistence.InMemory
{
    /// <summary>
    /// Represents an in-memory persistence engine.
    /// </summary>
    public class InMemoryPersistenceEngine : IPersistStreams
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
        private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
        // Keep a process-wide checkpoint-ordered index so global checkpoint reads do not need
        // to flatten every bucket and sort the combined result on each query.
        private readonly List<InMemoryCommit> _commitsByCheckpoint = [];
#if NET9_0_OR_GREATER
        private readonly Lock _commitsByCheckpointSync = new();
#else
        private readonly object _commitsByCheckpointSync = new();
#endif
        private bool _disposed;
        private long _checkpoint;

        private Bucket this[string bucketId]
        {
            get { return _buckets.GetOrAdd(bucketId, _ => new Bucket()); }
        }

        /// <summary>
        /// Disposes the engine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes the engine.
        /// </summary>
        public void Initialize()
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.InitializingEngine);
            }
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFrom(Int64 checkpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime startDate)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromTime, bucketId, startDate);
            }
            return this[bucketId].GetFrom(startDate);
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromToTime, startDate, endDate);
            }
            return this[bucketId].GetFromTo(startDate, endDate);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromCheckpoint, checkpointToken);
            }

            lock (_commitsByCheckpointSync)
            {
                return GetCheckpointRange(_commitsByCheckpoint, checkpointToken, long.MaxValue);
            }
        }

        /// <inheritdoc/>
        public Task GetFromAsync(Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return ObserveDataStream(() => GetFrom(checkpointToken), asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingCommitsFromToCheckpoint, fromCheckpointToken, toCheckpointToken);
            }

            lock (_commitsByCheckpointSync)
            {
                return GetCheckpointRange(_commitsByCheckpoint, fromCheckpointToken, toCheckpointToken);
            }
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(Int64 fromCheckpointToken, Int64 toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return ObserveDataStream(() => GetFromTo(fromCheckpointToken, toCheckpointToken), asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromBucketAndCheckpoint, bucketId, checkpointToken);
            }
            return this[bucketId].GetFrom(checkpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return ObserveDataStream(() => GetFrom(bucketId, checkpointToken), asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingCommitsFromBucketAndFromToCheckpoint, bucketId, fromCheckpointToken, toCheckpointToken);
            }
            return this[bucketId].GetFromTo(fromCheckpointToken, toCheckpointToken);
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(string bucketId, long fromCheckpointToken, long toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            return ObserveDataStream(() => GetFromTo(bucketId, fromCheckpointToken, toCheckpointToken), asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromRevision, streamId, bucketId, minRevision, maxRevision);
            }
            return this[bucketId].GetFrom(streamId, minRevision, maxRevision);
        }

        private long NextCheckpoint()
        {
            return Interlocked.Increment(ref _checkpoint);
        }

        /// <inheritdoc/>
        public ICommit? Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.BucketId, attempt.CommitSequence);
            }
            var commit = this[attempt.BucketId].Commit(attempt, NextCheckpoint);
            RegisterCommit(commit);
            return commit;
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            return ObserveDataStream(() => GetFrom(bucketId, streamId, minRevision, maxRevision), observer, cancellationToken);
        }

        private static async Task ObserveDataStream<T>(Func<IEnumerable<T>> dataProvider, IAsyncObserver<T> observer, CancellationToken cancellationToken)
        {
            try
            {
                var data = dataProvider();
                if (data?.Any() == true)
                {
                    foreach (var commit in data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Operation Cancellation Requested");
                        }
                        var goOn = await observer.OnNextAsync(commit, cancellationToken).ConfigureAwait(false);
                        if (!goOn)
                        {
                            break;
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Operation Cancellation Requested");
                        }
                    }
                }
                await observer.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cooperative cancellation.
            }
            catch (Exception ex)
            {
                await observer.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            return Task.FromResult(Commit(attempt));
        }

        /// <inheritdoc/>
        public ISnapshot? GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingSnapshotForStream, bucketId, streamId, maxRevision);
            }
            return this[bucketId].GetSnapshot(streamId, maxRevision);
        }

        /// <inheritdoc/>
        public bool AddSnapshot(ISnapshot snapshot)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.AddingSnapshot, snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision);
            }
            return this[snapshot.BucketId].AddSnapshot(snapshot);
        }

        /// <inheritdoc/>
        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingStreamsToSnapshot, bucketId, maxThreshold);
            }
            return this[bucketId].GetStreamsToSnapshot(maxThreshold);
        }

        /// <inheritdoc/>
        public Task<ISnapshot?> GetSnapshotAsync(string bucketId, string streamId, int maxRevision, CancellationToken cancellationToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingSnapshotForStream, bucketId, streamId, maxRevision);
            }
            return Task.FromResult(this[bucketId].GetSnapshot(streamId, maxRevision));
        }

        /// <inheritdoc/>
        public Task<bool> AddSnapshotAsync(ISnapshot snapshot, CancellationToken cancellationToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.AddingSnapshot, snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision);
            }
            return Task.FromResult(this[snapshot.BucketId].AddSnapshot(snapshot));
        }

        /// <inheritdoc/>
        public async Task GetStreamsToSnapshotAsync(string bucketId, int maxThreshold, IAsyncObserver<IStreamHead> asyncObserver, CancellationToken cancellationToken)
        {
            try
            {
                var data = GetStreamsToSnapshot(bucketId, maxThreshold);
                if (data?.Any() == true)
                {
                    foreach (var commit in data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Operation Cancellation Requested");
                        }
                        var goOn = await asyncObserver.OnNextAsync(commit, cancellationToken).ConfigureAwait(false);
                        if (!goOn)
                        {
                            break;
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Operation Cancellation Requested");
                        }
                    }
                }
                await asyncObserver.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await asyncObserver.OnErrorAsync(ex, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public void Purge()
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(Resources.PurgingStore);
            }

            foreach (var bucket in _buckets.Values)
            {
                bucket.Purge();
            }

            lock (_commitsByCheckpointSync)
            {
                _commitsByCheckpoint.Clear();
            }

            Interlocked.Exchange(ref _checkpoint, 0);
        }

        /// <inheritdoc/>
        public void Purge(string bucketId)
        {
            ThrowWhenDisposed();
            if (_buckets.TryRemove(bucketId, out Bucket? bucket))
            {
                RemoveGlobalCommits(bucket.Purge());
            }
        }

        /// <inheritdoc/>
        public Task PurgeAsync(CancellationToken cancellationToken)
        {
            Purge();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PurgeAsync(string bucketId, CancellationToken cancellationToken)
        {
            Purge(bucketId);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Drop()
        {
            ThrowWhenDisposed();

            _buckets.Clear();

            lock (_commitsByCheckpointSync)
            {
                _commitsByCheckpoint.Clear();
            }

            Interlocked.Exchange(ref _checkpoint, 0);
        }

        /// <inheritdoc/>
        public void DeleteStream(string bucketId, string streamId)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(Resources.DeletingStream, streamId, bucketId);
            }
            if (!_buckets.TryGetValue(bucketId, out Bucket? bucket))
            {
                return;
            }
            RemoveGlobalCommits(bucket.DeleteStream(streamId));
        }

        /// <inheritdoc/>
        public Task DeleteStreamAsync(string bucketId, string streamId, CancellationToken cancellationToken)
        {
            DeleteStream(bucketId, streamId);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool IsDisposed
        {
            get { return _disposed; }
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        private void Dispose(bool disposing)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
        {
            _disposed = true;
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.DisposingEngine);
            }
        }

        private void ThrowWhenDisposed()
        {
            if (!_disposed)
            {
                return;
            }
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(Resources.AlreadyDisposed);
            }
            throw new ObjectDisposedException(Resources.AlreadyDisposed);
        }

        private void RegisterCommit(InMemoryCommit commit)
        {
            lock (_commitsByCheckpointSync)
            {
                // Checkpoints are monotonically increasing, but use a binary-search insert point
                // so the index stays correct even if the storage strategy changes later.
                int insertIndex = FindCheckpointInsertIndex(_commitsByCheckpoint, commit.CheckpointToken);
                _commitsByCheckpoint.Insert(insertIndex, commit);
            }
        }

        private void RemoveGlobalCommits(IReadOnlyCollection<InMemoryCommit> commits)
        {
            if (commits.Count == 0)
            {
                return;
            }

            lock (_commitsByCheckpointSync)
            {
                // Delete and purge operations must update the global checkpoint index as well,
                // otherwise later reads would return stale commits that no longer exist in a bucket.
                foreach (var commit in commits)
                {
                    int index = FindCommitIndex(_commitsByCheckpoint, commit.CheckpointToken);
                    if (index >= 0)
                    {
                        _commitsByCheckpoint.RemoveAt(index);
                    }
                }
            }
        }

        private static ICommit[] GetCheckpointRange(List<InMemoryCommit> commits, long fromCheckpointExclusive, long toCheckpointInclusive)
        {
            // Reads are defined as (from, to], so find the first commit strictly after the lower
            // bound and the first commit strictly after the upper bound, then copy the slice.
            int startIndex = FindFirstCheckpointAfter(commits, fromCheckpointExclusive);
            if (startIndex >= commits.Count)
            {
                return [];
            }

            int endIndex = FindFirstCheckpointAfter(commits, toCheckpointInclusive);
            int count = endIndex - startIndex;
            if (count <= 0)
            {
                return [];
            }

            var results = new ICommit[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = commits[startIndex + i];
            }

            return results;
        }

        private static int FindFirstCheckpointAfter(List<InMemoryCommit> commits, long checkpoint)
        {
            int low = 0;
            int high = commits.Count;

            while (low < high)
            {
                int mid = low + ((high - low) / 2);
                if (commits[mid].CheckpointToken <= checkpoint)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private static int FindCheckpointInsertIndex(List<InMemoryCommit> commits, long checkpoint)
        {
            int low = 0;
            int high = commits.Count;

            while (low < high)
            {
                int mid = low + ((high - low) / 2);
                if (commits[mid].CheckpointToken < checkpoint)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private static int FindCommitIndex(List<InMemoryCommit> commits, long checkpoint)
        {
            int low = 0;
            int high = commits.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                long currentCheckpoint = commits[mid].CheckpointToken;
                if (currentCheckpoint == checkpoint)
                {
                    return mid;
                }

                if (currentCheckpoint < checkpoint)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return -1;
        }

        private class InMemoryCommit : Commit
        {
            public InMemoryCommit(
                string bucketId,
                string streamId,
                int streamRevision,
                Guid commitId,
                int commitSequence,
                DateTime commitStamp,
                Int64 checkpointToken,
                IDictionary<string, object> headers,
                ICollection<EventMessage> events)
                : base(bucketId, streamId, streamRevision, commitId, commitSequence, commitStamp, checkpointToken, headers, events)
            { }
        }

        private class IdentityForConcurrencyConflictDetection
        {
            protected bool Equals(IdentityForConcurrencyConflictDetection other)
            {
                return string.Equals(this.streamId, other.streamId, StringComparison.Ordinal)
                        && string.Equals(this.bucketId, other.bucketId, StringComparison.Ordinal)
                        && this.commitSequence == other.commitSequence;
            }

            public override bool Equals(object? obj)
            {
                if (obj is null)
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((IdentityForConcurrencyConflictDetection)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = this.streamId.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.bucketId.GetHashCode();
                    return (hashCode * 397) ^ this.commitSequence;
                }
            }

            private readonly int commitSequence;

            private readonly string bucketId;

            private readonly string streamId;

            public IdentityForConcurrencyConflictDetection(CommitAttempt commitAttempt)
            {
                bucketId = commitAttempt.BucketId;
                streamId = commitAttempt.StreamId;
                commitSequence = commitAttempt.CommitSequence;
            }

            public IdentityForConcurrencyConflictDetection(Commit commit)
            {
                bucketId = commit.BucketId;
                streamId = commit.StreamId;
                commitSequence = commit.CommitSequence;
            }
        }

        private class IdentityForDuplicationDetection
        {
            protected bool Equals(IdentityForDuplicationDetection other)
            {
                return string.Equals(this.streamId, other.streamId, StringComparison.Ordinal)
                        && string.Equals(this.bucketId, other.bucketId, StringComparison.Ordinal)
                        && this.commitId.Equals(other.commitId);
            }

            public override bool Equals(object? obj)
            {
                if (obj is null)
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((IdentityForDuplicationDetection)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = this.streamId.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.bucketId.GetHashCode();
                    return (hashCode * 397) ^ this.commitId.GetHashCode();
                }
            }

            private readonly Guid commitId;

            private readonly string bucketId;

            private readonly string streamId;

            public IdentityForDuplicationDetection(CommitAttempt commitAttempt)
            {
                bucketId = commitAttempt.BucketId;
                streamId = commitAttempt.StreamId;
                commitId = commitAttempt.CommitId;
            }

            public IdentityForDuplicationDetection(Commit commit)
            {
                bucketId = commit.BucketId;
                streamId = commit.StreamId;
                commitId = commit.CommitId;
            }
        }

        private class Bucket
        {
#if NET9_0_OR_GREATER
            private readonly Lock _sync = new();
#else
            private readonly object _sync = new();
#endif
            // Maintain bucket-local commits in checkpoint order for checkpoint paging APIs.
            private readonly List<InMemoryCommit> _commitsByCheckpoint = [];
            // Maintain per-stream commits ordered by StreamRevision so stream reads can jump
            // directly to the relevant commit range instead of scanning the entire bucket.
            private readonly Dictionary<string, List<InMemoryCommit>> _commitsByStreamId = [];
            private readonly HashSet<IdentityForDuplicationDetection> _potentialDuplicates = [];
            private readonly HashSet<IdentityForConcurrencyConflictDetection> _potentialConflicts = [];

            // Stream-head reads and updates are keyed by StreamId, so keep a direct dictionary
            // instead of repeatedly scanning a linked list. We still replace the entire StreamHead
            // object on each update so callers always observe an immutable snapshot of the stream's
            // current metadata instead of a mutable shared instance.
            private readonly Dictionary<string, StreamHead> _headsByStreamId = [];
            // Snapshots are also queried by StreamId first and then by the greatest revision that is
            // <= maxRevision. Grouping them per stream lets us avoid scanning snapshots that belong
            // to unrelated streams, while keeping each stream's list sorted by StreamRevision so the
            // "latest prior snapshot" lookup remains a binary-search operation.
            private readonly Dictionary<string, List<ISnapshot>> _snapshotsByStreamId = [];
            private readonly Dictionary<Guid, DateTime> _stamps = [];

            public IEnumerable<ICommit> GetFrom(string streamId, int minRevision, int maxRevision)
            {
                lock (_sync)
                {
                    if (!_commitsByStreamId.TryGetValue(streamId, out List<InMemoryCommit>? commits))
                    {
                        return [];
                    }

                    int startIndex = FindFirstCommitEndingAtOrAfterRevision(commits, minRevision);
                    if (startIndex >= commits.Count)
                    {
                        return [];
                    }

                    // Commits remain ordered by StreamRevision, so once a commit starts beyond the
                    // requested max revision the rest of the stream will also be out of range.
                    var results = new List<ICommit>(commits.Count - startIndex);
                    for (int i = startIndex; i < commits.Count; i++)
                    {
                        InMemoryCommit commit = commits[i];
                        if ((commit.StreamRevision - commit.Events.Count + 1) > maxRevision)
                        {
                            break;
                        }

                        results.Add(commit);
                    }

                    return results.ToArray();
                }
            }

            public IEnumerable<ICommit> GetFrom(DateTime start)
            {
                Guid commitId = _stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
                if (commitId == Guid.Empty)
                {
                    return [];
                }

                lock (_sync)
                {
                    InMemoryCommit? startingCommit = _commitsByCheckpoint.FirstOrDefault(x => x.CommitId == commitId);
                    if (startingCommit == null)
                    {
                        return [];
                    }

                    int startIndex = FindCheckpointInsertIndex(_commitsByCheckpoint, startingCommit.CheckpointToken);
                    return CopyCheckpointRange(_commitsByCheckpoint, startIndex, _commitsByCheckpoint.Count);
                }
            }

            public IEnumerable<ICommit> GetFrom(Int64 checkpoint)
            {
                lock (_sync)
                {
                    int startIndex = FindFirstCheckpointAfter(_commitsByCheckpoint, checkpoint);
                    return CopyCheckpointRange(_commitsByCheckpoint, startIndex, _commitsByCheckpoint.Count);
                }
            }

            public IEnumerable<ICommit> GetFromTo(Int64 from, Int64 to)
            {
                lock (_sync)
                {
                    int startIndex = FindFirstCheckpointAfter(_commitsByCheckpoint, from);
                    int endIndex = FindFirstCheckpointAfter(_commitsByCheckpoint, to);
                    return CopyCheckpointRange(_commitsByCheckpoint, startIndex, endIndex);
                }
            }

            public IEnumerable<ICommit> GetFromTo(DateTime start, DateTime end)
            {
                IEnumerable<Guid> selectedCommitIds = _stamps.Where(x => x.Value >= start && x.Value < end).Select(x => x.Key).ToArray();
                Guid firstCommitId = selectedCommitIds.FirstOrDefault();
                Guid lastCommitId = selectedCommitIds.LastOrDefault();
                if (firstCommitId == Guid.Empty && lastCommitId == Guid.Empty)
                {
                    return [];
                }
                lock (_sync)
                {
                    InMemoryCommit? startingCommit = _commitsByCheckpoint.FirstOrDefault(x => x.CommitId == firstCommitId);
                    InMemoryCommit? endingCommit = _commitsByCheckpoint.FirstOrDefault(x => x.CommitId == lastCommitId);
                    int startingCommitIndex = startingCommit == null ? 0 : FindCheckpointInsertIndex(_commitsByCheckpoint, startingCommit.CheckpointToken);
                    int endingCommitIndex = endingCommit == null ? _commitsByCheckpoint.Count - 1 : FindCheckpointInsertIndex(_commitsByCheckpoint, endingCommit.CheckpointToken);
                    return CopyCheckpointRange(_commitsByCheckpoint, startingCommitIndex, endingCommitIndex + 1);
                }
            }

            public InMemoryCommit Commit(CommitAttempt attempt, Func<long> nextCheckpoint)
            {
                lock (_sync)
                {
                    DetectDuplicate(attempt);
                    if (_potentialConflicts.Contains(new IdentityForConcurrencyConflictDetection(attempt)))
                    {
                        throw new ConcurrencyException();
                    }
                    long checkpoint = nextCheckpoint();
                    var commit = new InMemoryCommit(attempt.BucketId,
                        attempt.StreamId,
                        attempt.StreamRevision,
                        attempt.CommitId,
                        attempt.CommitSequence,
                        attempt.CommitStamp,
                        checkpoint,
                        attempt.Headers,
                        attempt.Events);
                    
                    _stamps[commit.CommitId] = commit.CommitStamp;
                    InsertCommit(commit);
                    _potentialDuplicates.Add(new IdentityForDuplicationDetection(commit));
                    _potentialConflicts.Add(new IdentityForConcurrencyConflictDetection(commit));
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(Resources.UpdatingStreamHead, commit.StreamId, commit.BucketId);
                    }

                    // A commit advances the head revision but must preserve whatever snapshot
                    // revision was already recorded for the stream. Resetting that value would make
                    // GetStreamsToSnapshot believe the stream has never been snapshotted.
                    int snapshotRevision = _headsByStreamId.TryGetValue(commit.StreamId, out StreamHead? head)
                        ? head.SnapshotRevision
                        : 0;
                    UpdateStreamHead(commit.BucketId, commit.StreamId, commit.StreamRevision, snapshotRevision);
                    return commit;
                }
            }

            private void DetectDuplicate(CommitAttempt attempt)
            {
                if (_potentialDuplicates.Contains(new IdentityForDuplicationDetection(attempt)))
                {
                    throw new DuplicateCommitException(String.Format(
                        CultureInfo.InvariantCulture,
                        Messages.DuplicateCommitIdException, attempt.StreamId, attempt.BucketId, attempt.CommitId));
                }
            }

            public IEnumerable<IStreamHead> GetStreamsToSnapshot(int maxThreshold)
            {
                lock (_sync)
                {
                    // Materialize the result while holding the lock. Returning a deferred iterator
                    // over the internal dictionary would expose callers to collection mutation after
                    // the lock is released and would break the multi-thread-safe IPersistStreams
                    // contract.
                    var streamsToSnapshot = new List<IStreamHead>(_headsByStreamId.Count);
                    foreach (StreamHead stream in _headsByStreamId.Values)
                    {
                        if (stream.HeadRevision >= stream.SnapshotRevision + maxThreshold)
                        {
                            streamsToSnapshot.Add(new StreamHead(stream.BucketId, stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
                        }
                    }

                    return streamsToSnapshot.ToArray();
                }
            }

            public ISnapshot? GetSnapshot(string streamId, int maxRevision)
            {
                lock (_sync)
                {
                    if (!_snapshotsByStreamId.TryGetValue(streamId, out List<ISnapshot>? snapshots))
                    {
                        return null;
                    }

                    int snapshotIndex = FindLastSnapshotAtOrBeforeRevision(snapshots, maxRevision);
                    return snapshotIndex < 0 ? null : snapshots[snapshotIndex];
                }
            }

            public bool AddSnapshot(ISnapshot snapshot)
            {
                lock (_sync)
                {
                    if (!_headsByStreamId.TryGetValue(snapshot.StreamId, out StreamHead? currentHead))
                    {
                        return false;
                    }

                    if (!_snapshotsByStreamId.TryGetValue(snapshot.StreamId, out List<ISnapshot>? snapshots))
                    {
                        snapshots = [];
                        _snapshotsByStreamId.Add(snapshot.StreamId, snapshots);
                    }

                    // if the snapshot is already there do NOT add it (follow the SQL implementation)
                    // and the original GetSnapshot behavior which was to return the first one that was
                    // added to the collection
                    int snapshotIndex = FindSnapshotInsertIndex(snapshots, snapshot.StreamRevision);
                    if (snapshotIndex < snapshots.Count && snapshots[snapshotIndex].StreamRevision == snapshot.StreamRevision)
                    {
                        return false;
                    }

                    snapshots.Insert(snapshotIndex, snapshot);

                    // Preserve the historic behavior where adding any snapshot, even an older one,
                    // updates the stream head's SnapshotRevision to that exact revision.
                    UpdateStreamHead(currentHead.BucketId, currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision);
                }
                return true;
            }

            public IReadOnlyCollection<InMemoryCommit> Purge()
            {
                lock (_sync)
                {
                    var removedCommits = _commitsByCheckpoint.ToArray();
                    _commitsByCheckpoint.Clear();
                    _commitsByStreamId.Clear();
                    // Purge is the hard reset path for the bucket, so every secondary index must be
                    // cleared together with the primary commit indexes. Leaving any of them behind
                    // would surface stale stream heads or snapshots after the bucket is supposedly empty.
                    _snapshotsByStreamId.Clear();
                    _headsByStreamId.Clear();
                    _stamps.Clear();
                    _potentialConflicts.Clear();
                    _potentialDuplicates.Clear();
                    return removedCommits;
                }
            }

            public IReadOnlyCollection<InMemoryCommit> DeleteStream(string streamId)
            {
                lock (_sync)
                {
                    if (!_commitsByStreamId.TryGetValue(streamId, out List<InMemoryCommit>? commitsForStream))
                    {
                        return [];
                    }

                    var commits = commitsForStream.ToArray();
                    foreach (var commit in commits)
                    {
                        int checkpointIndex = FindCommitIndex(_commitsByCheckpoint, commit.CheckpointToken);
                        if (checkpointIndex >= 0)
                        {
                            _commitsByCheckpoint.RemoveAt(checkpointIndex);
                        }

                        _stamps.Remove(commit.CommitId);
                    }

                    _commitsByStreamId.Remove(streamId);
                    // Stream deletion must remove every metadata shortcut for that stream in the
                    // same critical section as the commit removal. Otherwise later lookups could
                    // still find a head or snapshot that no longer has backing commits.
                    _snapshotsByStreamId.Remove(streamId);
                    _headsByStreamId.Remove(streamId);

                    return commits;
                }
            }

            private void UpdateStreamHead(string bucketId, string streamId, int headRevision, int snapshotRevision)
            {
                // Store a fresh value object for each update so callers never get a reference that
                // can be mutated behind their back, and so head reads always reflect a coherent pair
                // of HeadRevision and SnapshotRevision values from the same write.
                _headsByStreamId[streamId] = new StreamHead(bucketId, streamId, headRevision, snapshotRevision);
            }

            private void InsertCommit(InMemoryCommit commit)
            {
                // Update both indexes under the same bucket lock so checkpoint reads and
                // stream-revision reads always observe the same commit set.
                int checkpointIndex = FindCheckpointInsertIndex(_commitsByCheckpoint, commit.CheckpointToken);
                _commitsByCheckpoint.Insert(checkpointIndex, commit);

                if (!_commitsByStreamId.TryGetValue(commit.StreamId, out List<InMemoryCommit>? streamCommits))
                {
                    streamCommits = [];
                    _commitsByStreamId.Add(commit.StreamId, streamCommits);
                }

                int streamIndex = FindStreamRevisionInsertIndex(streamCommits, commit.StreamRevision);
                streamCommits.Insert(streamIndex, commit);
            }

            private static ICommit[] CopyCheckpointRange(List<InMemoryCommit> commits, int startIndex, int endIndexExclusive)
            {
                int count = endIndexExclusive - startIndex;
                if (count <= 0)
                {
                    return [];
                }

                var results = new ICommit[count];
                for (int i = 0; i < count; i++)
                {
                    results[i] = commits[startIndex + i];
                }

                return results;
            }

            private static int FindFirstCommitEndingAtOrAfterRevision(List<InMemoryCommit> commits, int revision)
            {
                // The stream index is sorted by each commit's ending revision, which is enough for
                // the existing read contract because a commit belongs in the result if its ending
                // revision is >= minRevision and its starting revision is <= maxRevision.
                int low = 0;
                int high = commits.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (commits[mid].StreamRevision < revision)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }

            private static int FindStreamRevisionInsertIndex(List<InMemoryCommit> commits, int revision)
            {
                int low = 0;
                int high = commits.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (commits[mid].StreamRevision < revision)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }

            private static int FindSnapshotInsertIndex(List<ISnapshot> snapshots, int revision)
            {
                int low = 0;
                int high = snapshots.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (snapshots[mid].StreamRevision < revision)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }

            private static int FindLastSnapshotAtOrBeforeRevision(List<ISnapshot> snapshots, int revision)
            {
                int low = 0;
                int high = snapshots.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (snapshots[mid].StreamRevision <= revision)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low - 1;
            }

            private static int FindFirstCheckpointAfter(List<InMemoryCommit> commits, long checkpoint)
            {
                int low = 0;
                int high = commits.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (commits[mid].CheckpointToken <= checkpoint)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }

            private static int FindCheckpointInsertIndex(List<InMemoryCommit> commits, long checkpoint)
            {
                int low = 0;
                int high = commits.Count;

                while (low < high)
                {
                    int mid = low + ((high - low) / 2);
                    if (commits[mid].CheckpointToken < checkpoint)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                return low;
            }

            private static int FindCommitIndex(List<InMemoryCommit> commits, long checkpoint)
            {
                int low = 0;
                int high = commits.Count - 1;

                while (low <= high)
                {
                    int mid = low + ((high - low) / 2);
                    long currentCheckpoint = commits[mid].CheckpointToken;
                    if (currentCheckpoint == checkpoint)
                    {
                        return mid;
                    }

                    if (currentCheckpoint < checkpoint)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }

                return -1;
            }
        }
    }
}
