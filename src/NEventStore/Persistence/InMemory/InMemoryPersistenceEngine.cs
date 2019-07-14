namespace NEventStore.Persistence.InMemory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NEventStore.Logging;

    public class InMemoryPersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
        private readonly ConcurrentDictionary<string, Bucket> _buckets = new ConcurrentDictionary<string, Bucket>();
        private bool _disposed;
        private int _checkpoint;

        private Bucket this[string bucketId]
        {
            get { return _buckets.GetOrAdd(bucketId, _ => new Bucket()); }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            if (Logger.IsInfoEnabled) Logger.Info(Resources.InitializingEngine);
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingAllCommitsFromRevision, streamId, minRevision, maxRevision);
            return this[bucketId].GetFrom(streamId, minRevision, maxRevision);
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingAllCommitsFromTime, bucketId, start);
            return this[bucketId].GetFrom(start);
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingAllCommitsFromBucketAndCheckpoint, bucketId, checkpointToken);
            return this[bucketId].GetFrom(checkpointToken);
        }

        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 from, Int64 to)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingCommitsFromBucketAndFromToCheckpoint, bucketId, from, to);
            return this[bucketId].GetFromTo(from, to);
        }

        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingAllCommitsFromCheckpoint, checkpointToken);
            return _buckets
                .Values
                .SelectMany(b => b.GetCommits())
                .Where(c => c.CheckpointToken.CompareTo(checkpointToken) > 0)
                .OrderBy(c => c.CheckpointToken)
                .ToArray();
        }

        public IEnumerable<ICommit> GetFromTo(Int64 from, Int64 to)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingCommitsFromToCheckpoint, from, to);
            return _buckets
                .Values
                .SelectMany(b => b.GetCommits())
                .Where(c => c.CheckpointToken.CompareTo(from) > 0 && c.CheckpointToken.CompareTo(to) <= 0)
                .OrderBy(c => c.CheckpointToken)
                .ToArray();
        }

        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingAllCommitsFromToTime, start, end);
            return this[bucketId].GetFromTo(start, end);
        }

        public ICommit Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);
            return this[attempt.BucketId].Commit(attempt, Interlocked.Increment(ref _checkpoint));
        }

        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingStreamsToSnapshot, bucketId, maxThreshold);
            return this[bucketId].GetStreamsToSnapshot(maxThreshold);
        }

        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.GettingSnapshotForStream, bucketId, streamId, maxRevision);
            return this[bucketId].GetSnapshot(streamId, maxRevision);
        }

        public bool AddSnapshot(ISnapshot snapshot)
        {
            ThrowWhenDisposed();
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);
            return this[snapshot.BucketId].AddSnapshot(snapshot);
        }

        public void Purge()
        {
            ThrowWhenDisposed();
            if (Logger.IsWarnEnabled) Logger.Warn(Resources.PurgingStore);
            foreach (var bucket in _buckets.Values)
            {
                bucket.Purge();
            }
        }

        public void Purge(string bucketId)
        {
            Bucket _;
            _buckets.TryRemove(bucketId, out _);
        }

        public void Drop()
        {
            _buckets.Clear();
        }

        public void DeleteStream(string bucketId, string streamId)
        {
            if (Logger.IsWarnEnabled) Logger.Warn(Resources.DeletingStream, streamId, bucketId);
            if (!_buckets.TryGetValue(bucketId, out Bucket bucket))
            {
                return;
            }
            bucket.DeleteStream(streamId);
        }

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
            if (Logger.IsInfoEnabled) Logger.Info(Resources.DisposingEngine);
        }

        private void ThrowWhenDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            if (Logger.IsWarnEnabled) Logger.Warn(Resources.AlreadyDisposed);
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
                IEnumerable<EventMessage> events)
                : base(bucketId, streamId, streamRevision, commitId, commitSequence, commitStamp, checkpointToken, headers, events)
            { }
        }

        private class IdentityForConcurrencyConflictDetection
        {
            protected bool Equals(IdentityForConcurrencyConflictDetection other)
            {
                return string.Equals(this.streamId, other.streamId)
                        && string.Equals(this.bucketId, other.bucketId)
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
                return string.Equals(this.streamId, other.streamId)
                        && string.Equals(this.bucketId, other.bucketId)
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
            private readonly IList<InMemoryCommit> _commits = new List<InMemoryCommit>();
            private readonly ICollection<IdentityForDuplicationDetection> _potentialDuplicates = new HashSet<IdentityForDuplicationDetection>();
            private readonly ICollection<IdentityForConcurrencyConflictDetection> _potentialConflicts = new HashSet<IdentityForConcurrencyConflictDetection>();

            public IEnumerable<InMemoryCommit> GetCommits()
            {
                lock (_commits)
                {
                    return _commits.ToArray();
                }
            }

            private readonly ICollection<IStreamHead> _heads = new LinkedList<IStreamHead>();
            private readonly ICollection<ISnapshot> _snapshots = new LinkedList<ISnapshot>();
            private readonly IDictionary<Guid, DateTime> _stamps = new Dictionary<Guid, DateTime>();

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
                    return Enumerable.Empty<ICommit>();
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
                    return Enumerable.Empty<ICommit>();
                }
                InMemoryCommit startingCommit = _commits.FirstOrDefault(x => x.CommitId == firstCommitId);
                InMemoryCommit endingCommit = _commits.FirstOrDefault(x => x.CommitId == lastCommitId);
                int startingCommitIndex = (startingCommit == null) ? 0 : _commits.IndexOf(startingCommit);
                int endingCommitIndex = (endingCommit == null) ? _commits.Count - 1 : _commits.IndexOf(endingCommit);
                int numberToTake = endingCommitIndex - startingCommitIndex + 1;

                return _commits.Skip(_commits.IndexOf(startingCommit)).Take(numberToTake);
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
                    if (Logger.IsDebugEnabled) Logger.Debug(Resources.UpdatingStreamHead, commit.StreamId);
                    int snapshotRevision = head?.SnapshotRevision ?? 0;
                    _heads.Add(new StreamHead(commit.BucketId, commit.StreamId, commit.StreamRevision, snapshotRevision));
                    return commit;
                }
            }

            private void DetectDuplicate(CommitAttempt attempt)
            {
                if (_potentialDuplicates.Contains(new IdentityForDuplicationDetection(attempt)))
                {
                    throw new DuplicateCommitException(String.Format(Messages.DuplicateCommitIdException, attempt.StreamId, attempt.BucketId, attempt.CommitId));
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