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
        private bool _disposed;
        private int _checkpoint;

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
            return _buckets
                .Values
                .SelectMany(b => b.GetCommits())
                .Where(c => c.CheckpointToken.CompareTo(checkpointToken) > 0)
                .OrderBy(c => c.CheckpointToken)
                .ToArray();
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingCommitsFromToCheckpoint, fromCheckpointToken, toCheckpointToken);
            }
            return _buckets
                .Values
                .SelectMany(b => b.GetCommits())
                .Where(c => c.CheckpointToken.CompareTo(fromCheckpointToken) > 0 && c.CheckpointToken.CompareTo(toCheckpointToken) <= 0)
                .OrderBy(c => c.CheckpointToken)
                .ToArray();
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
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.GettingAllCommitsFromRevision, streamId, bucketId, minRevision, maxRevision);
            }
            return this[bucketId].GetFrom(streamId, minRevision, maxRevision);
        }

        /// <inheritdoc/>
        public ICommit? Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.BucketId, attempt.CommitSequence);
            }
            return this[attempt.BucketId].Commit(attempt, Interlocked.Increment(ref _checkpoint));
        }

        /// <inheritdoc/>
        public async Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            var data = GetFrom(bucketId, streamId, minRevision, maxRevision);
            if (data?.Any() == true)
            {
                foreach (var commit in data)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await observer.OnErrorAsync(commit.CheckpointToken, new OperationCanceledException());
                        break;
                    }
                    await observer.OnNextAsync(commit);
                }
                await observer.OnCompletedAsync(data.Last().CheckpointToken);
            }
        }

        /// <inheritdoc/>
        public Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            return Task.FromResult(Commit(attempt));
        }

        /// <inheritdoc/>
        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
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
        public Task<ISnapshot> GetSnapshotAsync(string bucketId, string streamId, int maxRevision, CancellationToken cancellationToken)
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
            var data = GetStreamsToSnapshot(bucketId, maxThreshold);
            if (data?.Any() == true)
            {
                foreach (var commit in data)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await asyncObserver.OnErrorAsync(0, new OperationCanceledException());
                        break;
                    }
                    await asyncObserver.OnNextAsync(commit);
                }
                await asyncObserver.OnCompletedAsync(0);
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
        }

        /// <inheritdoc/>
        public void Purge(string bucketId)
        {
            _buckets.TryRemove(bucketId, out var _);
        }

        /// <inheritdoc/>
        public void Drop()
        {
            _buckets.Clear();
        }

        /// <inheritdoc/>
        public void DeleteStream(string bucketId, string streamId)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(Resources.DeletingStream, streamId, bucketId);
            }
            if (!_buckets.TryGetValue(bucketId, out Bucket bucket))
            {
                return;
            }
            bucket.DeleteStream(streamId);
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

            public override bool Equals(object obj)
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

            public override bool Equals(object obj)
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
            private readonly List<InMemoryCommit> _commits = [];
            private readonly HashSet<IdentityForDuplicationDetection> _potentialDuplicates = [];
            private readonly HashSet<IdentityForConcurrencyConflictDetection> _potentialConflicts = [];

            public IEnumerable<InMemoryCommit> GetCommits()
            {
                lock (_commits)
                {
                    return _commits.ToArray();
                }
            }

            private readonly ICollection<IStreamHead> _heads = new LinkedList<IStreamHead>();
            private readonly ICollection<ISnapshot> _snapshots = new LinkedList<ISnapshot>();
            private readonly Dictionary<Guid, DateTime> _stamps = [];

            public IEnumerable<ICommit> GetFrom(string streamId, int minRevision, int maxRevision)
            {
                lock (_commits)
                {
                    return _commits
                        .Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision)
                        .OrderBy(c => c.CommitSequence)
                        .ToArray();
                }
            }

            public IEnumerable<ICommit> GetFrom(DateTime start)
            {
                Guid commitId = _stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
                if (commitId == Guid.Empty)
                {
                    return [];
                }

                InMemoryCommit startingCommit = _commits.FirstOrDefault(x => x.CommitId == commitId);
                return _commits.Skip(_commits.IndexOf(startingCommit));
            }

            public IEnumerable<ICommit> GetFrom(Int64 checkpoint)
            {
                InMemoryCommit startingCommit = _commits.FirstOrDefault(x => x.CheckpointToken.CompareTo(checkpoint) == 0);
                return _commits.Skip(_commits.IndexOf(startingCommit) + 1 /* GetFrom => after the checkpoint*/);
            }

            public IEnumerable<ICommit> GetFromTo(Int64 from, Int64 to)
            {
                InMemoryCommit startingCommit = _commits.FirstOrDefault(x => x.CheckpointToken.CompareTo(from) == 0);
                return _commits.Skip(_commits.IndexOf(startingCommit) + 1 /* GetFrom => after the checkpoint*/)
                    .TakeWhile(c => c.CheckpointToken <= to);
            }

            public IEnumerable<ICommit> GetFromTo(DateTime start, DateTime end)
            {
                IEnumerable<Guid> selectedCommitIds = _stamps.Where(x => x.Value >= start && x.Value < end).Select(x => x.Key).ToArray();
                Guid firstCommitId = selectedCommitIds.FirstOrDefault();
                Guid lastCommitId = selectedCommitIds.LastOrDefault();
                if (lastCommitId == Guid.Empty && lastCommitId == Guid.Empty)
                {
                    return [];
                }
                InMemoryCommit startingCommit = _commits.FirstOrDefault(x => x.CommitId == firstCommitId);
                InMemoryCommit endingCommit = _commits.FirstOrDefault(x => x.CommitId == lastCommitId);
                int startingCommitIndex = (startingCommit == null) ? 0 : _commits.IndexOf(startingCommit);
                int endingCommitIndex = (endingCommit == null) ? _commits.Count - 1 : _commits.IndexOf(endingCommit);
                int numberToTake = endingCommitIndex - startingCommitIndex + 1;

                return _commits.Skip(startingCommitIndex).Take(numberToTake);
            }

            public ICommit Commit(CommitAttempt attempt, Int64 checkpoint)
            {
                lock (_commits)
                {
                    DetectDuplicate(attempt);
                    var commit = new InMemoryCommit(attempt.BucketId,
                        attempt.StreamId,
                        attempt.StreamRevision,
                        attempt.CommitId,
                        attempt.CommitSequence,
                        attempt.CommitStamp,
                        checkpoint,
                        attempt.Headers,
                        attempt.Events);
                    if (_potentialConflicts.Contains(new IdentityForConcurrencyConflictDetection(commit)))
                    {
                        throw new ConcurrencyException();
                    }
                    _stamps[commit.CommitId] = commit.CommitStamp;
                    _commits.Add(commit);
                    _potentialDuplicates.Add(new IdentityForDuplicationDetection(commit));
                    _potentialConflicts.Add(new IdentityForConcurrencyConflictDetection(commit));
                    IStreamHead head = _heads.FirstOrDefault(x => x.StreamId == commit.StreamId);
                    _heads.Remove(head);
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(Resources.UpdatingStreamHead, commit.StreamId, commit.BucketId);
                    }
                    int snapshotRevision = head?.SnapshotRevision ?? 0;
                    _heads.Add(new StreamHead(commit.BucketId, commit.StreamId, commit.StreamRevision, snapshotRevision));
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
                lock (_commits)
                {
                    return _heads
                        .Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
                        .Select(stream => new StreamHead(stream.BucketId, stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
                }
            }

            public ISnapshot GetSnapshot(string streamId, int maxRevision)
            {
                lock (_commits)
                {
                    return _snapshots
                        .Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
                        .OrderByDescending(x => x.StreamRevision)
                        .FirstOrDefault();
                }
            }

            public bool AddSnapshot(ISnapshot snapshot)
            {
                lock (_commits)
                {
                    IStreamHead currentHead = _heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
                    if (currentHead == null)
                    {
                        return false;
                    }

                    // if the snapshot is already there do NOT add it (follow the SQL implementation)
                    // and the original GetSnapshot behavior which was to return the first one that was
                    // added to the collection
                    if (_snapshots.Any(s => s.StreamId == snapshot.StreamId && s.StreamRevision == snapshot.StreamRevision))
                    {
                        return false;
                    }

                    _snapshots.Add(snapshot);
                    _heads.Remove(currentHead);
                    _heads.Add(new StreamHead(currentHead.BucketId, currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
                }
                return true;
            }

            public void Purge()
            {
                lock (_commits)
                {
                    _commits.Clear();
                    _snapshots.Clear();
                    _heads.Clear();
                    _potentialConflicts.Clear();
                    _potentialDuplicates.Clear();
                }
            }

            public void DeleteStream(string streamId)
            {
                lock (_commits)
                {
                    InMemoryCommit[] commits = _commits.Where(c => c.StreamId == streamId).ToArray();
                    foreach (var commit in commits)
                    {
                        _commits.Remove(commit);
                    }
                    ISnapshot[] snapshots = _snapshots.Where(s => s.StreamId == streamId).ToArray();
                    foreach (var snapshot in snapshots)
                    {
                        _snapshots.Remove(snapshot);
                    }
                    IStreamHead streamHead = _heads.SingleOrDefault(s => s.StreamId == streamId);
                    if (streamHead != null)
                    {
                        _heads.Remove(streamHead);
                    }
                }
            }
        }
    }
}