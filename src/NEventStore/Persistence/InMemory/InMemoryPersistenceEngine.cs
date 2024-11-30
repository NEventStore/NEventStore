#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

#endregion

namespace NEventStore.Persistence.InMemory;

public class InMemoryPersistenceEngine : IPersistStreams
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
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
        Logger.LogInformation(Resources.InitializingEngine);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingAllCommitsFromRevision, streamId, bucketId, minRevision, maxRevision);
        return this[bucketId].GetFrom(streamId, minRevision, maxRevision);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingAllCommitsFromTime, bucketId, start);
        return this[bucketId].GetFrom(start);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, long checkpointToken)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingAllCommitsFromBucketAndCheckpoint, bucketId, checkpointToken);
        return this[bucketId].GetFrom(checkpointToken);
    }

    public IEnumerable<ICommit> GetFromTo(string bucketId, long from, long to)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingCommitsFromBucketAndFromToCheckpoint, bucketId, from, to);
        return this[bucketId].GetFromTo(from, to);
    }

    public IEnumerable<ICommit> GetFrom(long checkpointToken)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingAllCommitsFromCheckpoint, checkpointToken);
        return _buckets
            .Values
            .SelectMany(b => b.GetCommits())
            .Where(c => c.CheckpointToken.CompareTo(checkpointToken) > 0)
            .OrderBy(c => c.CheckpointToken)
            .ToArray();
    }

    public IEnumerable<ICommit> GetFromTo(long from, long to)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingCommitsFromToCheckpoint, from, to);
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
        Logger.LogDebug(Resources.GettingAllCommitsFromToTime, start, end);
        return this[bucketId].GetFromTo(start, end);
    }

    public ICommit Commit(CommitAttempt attempt)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.BucketId,
            attempt.CommitSequence);
        return this[attempt.BucketId].Commit(attempt, Interlocked.Increment(ref _checkpoint));
    }

    public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingStreamsToSnapshot, bucketId, maxThreshold);
        return this[bucketId].GetStreamsToSnapshot(maxThreshold);
    }

    public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.GettingSnapshotForStream, bucketId, streamId, maxRevision);
        return this[bucketId].GetSnapshot(streamId, maxRevision);
    }

    public bool AddSnapshot(ISnapshot snapshot)
    {
        ThrowWhenDisposed();
        Logger.LogDebug(Resources.AddingSnapshot, snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision);
        return this[snapshot.BucketId].AddSnapshot(snapshot);
    }

    public void Purge()
    {
        ThrowWhenDisposed();
        Logger.LogWarning(Resources.PurgingStore);
        foreach (var bucket in _buckets.Values) bucket.Purge();
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
        Logger.LogWarning(Resources.DeletingStream, streamId, bucketId);
        if (!_buckets.TryGetValue(bucketId, out var bucket)) return;
        bucket.DeleteStream(streamId);
    }

    public bool IsDisposed { get; private set; }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
    private void Dispose(bool disposing)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
    {
        IsDisposed = true;
        Logger.LogInformation(Resources.DisposingEngine);
    }

    private void ThrowWhenDisposed()
    {
        if (!IsDisposed) return;

        Logger.LogWarning(Resources.AlreadyDisposed);
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
            long checkpointToken,
            IDictionary<string, object> headers,
            ICollection<EventMessage> events)
            : base(bucketId, streamId, streamRevision, commitId, commitSequence, commitStamp, checkpointToken,
                headers, events)
        {
        }
    }

    private class IdentityForConcurrencyConflictDetection
    {
        private readonly string bucketId;

        private readonly int commitSequence;

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

        protected bool Equals(IdentityForConcurrencyConflictDetection other)
        {
            return string.Equals(streamId, other.streamId)
                   && string.Equals(bucketId, other.bucketId)
                   && commitSequence == other.commitSequence;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IdentityForConcurrencyConflictDetection)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = streamId.GetHashCode();
                hashCode = (hashCode * 397) ^ bucketId.GetHashCode();
                return (hashCode * 397) ^ commitSequence;
            }
        }
    }

    private class IdentityForDuplicationDetection
    {
        private readonly string bucketId;

        private readonly Guid commitId;

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

        protected bool Equals(IdentityForDuplicationDetection other)
        {
            return string.Equals(streamId, other.streamId)
                   && string.Equals(bucketId, other.bucketId)
                   && commitId.Equals(other.commitId);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IdentityForDuplicationDetection)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = streamId.GetHashCode();
                hashCode = (hashCode * 397) ^ bucketId.GetHashCode();
                return (hashCode * 397) ^ commitId.GetHashCode();
            }
        }
    }

    private class Bucket
    {
        private readonly IList<InMemoryCommit> _commits = new List<InMemoryCommit>();

        private readonly ICollection<IStreamHead> _heads = new LinkedList<IStreamHead>();

        private readonly ICollection<IdentityForConcurrencyConflictDetection> _potentialConflicts =
            new HashSet<IdentityForConcurrencyConflictDetection>();

        private readonly ICollection<IdentityForDuplicationDetection> _potentialDuplicates =
            new HashSet<IdentityForDuplicationDetection>();

        private readonly ICollection<ISnapshot> _snapshots = new LinkedList<ISnapshot>();
        private readonly IDictionary<Guid, DateTime> _stamps = new Dictionary<Guid, DateTime>();

        public IEnumerable<InMemoryCommit> GetCommits()
        {
            lock (_commits)
            {
                return _commits.ToArray();
            }
        }

        public IEnumerable<ICommit> GetFrom(string streamId, int minRevision, int maxRevision)
        {
            lock (_commits)
            {
                return _commits
                    .Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision &&
                                x.StreamRevision - x.Events.Count + 1 <= maxRevision)
                    .OrderBy(c => c.CommitSequence)
                    .ToArray();
            }
        }

        public IEnumerable<ICommit> GetFrom(DateTime start)
        {
            var commitId = _stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
            if (commitId == Guid.Empty) return Enumerable.Empty<ICommit>();

            var startingCommit = _commits.FirstOrDefault(x => x.CommitId == commitId);
            return _commits.Skip(_commits.IndexOf(startingCommit));
        }

        public IEnumerable<ICommit> GetFrom(long checkpoint)
        {
            var startingCommit = _commits.FirstOrDefault(x => x.CheckpointToken.CompareTo(checkpoint) == 0);
            return _commits.Skip(_commits.IndexOf(startingCommit) + 1 /* GetFrom => after the checkpoint*/);
        }

        public IEnumerable<ICommit> GetFromTo(long from, long to)
        {
            var startingCommit = _commits.FirstOrDefault(x => x.CheckpointToken.CompareTo(from) == 0);
            return _commits.Skip(_commits.IndexOf(startingCommit) + 1 /* GetFrom => after the checkpoint*/)
                .TakeWhile(c => c.CheckpointToken <= to);
        }

        public IEnumerable<ICommit> GetFromTo(DateTime start, DateTime end)
        {
            IEnumerable<Guid> selectedCommitIds =
                _stamps.Where(x => x.Value >= start && x.Value < end).Select(x => x.Key).ToArray();
            var firstCommitId = selectedCommitIds.FirstOrDefault();
            var lastCommitId = selectedCommitIds.LastOrDefault();
            if (lastCommitId == Guid.Empty && lastCommitId == Guid.Empty) return Enumerable.Empty<ICommit>();
            var startingCommit = _commits.FirstOrDefault(x => x.CommitId == firstCommitId);
            var endingCommit = _commits.FirstOrDefault(x => x.CommitId == lastCommitId);
            var startingCommitIndex = startingCommit == null ? 0 : _commits.IndexOf(startingCommit);
            var endingCommitIndex = endingCommit == null ? _commits.Count - 1 : _commits.IndexOf(endingCommit);
            var numberToTake = endingCommitIndex - startingCommitIndex + 1;

            return _commits.Skip(_commits.IndexOf(startingCommit)).Take(numberToTake);
        }

        public ICommit Commit(CommitAttempt attempt, long checkpoint)
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
                    throw new ConcurrencyException();
                _stamps[commit.CommitId] = commit.CommitStamp;
                _commits.Add(commit);
                _potentialDuplicates.Add(new IdentityForDuplicationDetection(commit));
                _potentialConflicts.Add(new IdentityForConcurrencyConflictDetection(commit));
                var head = _heads.FirstOrDefault(x => x.StreamId == commit.StreamId);
                _heads.Remove(head);
                Logger.LogDebug(Resources.UpdatingStreamHead, commit.StreamId, commit.BucketId);
                var snapshotRevision = head?.SnapshotRevision ?? 0;
                _heads.Add(
                    new StreamHead(commit.BucketId, commit.StreamId, commit.StreamRevision, snapshotRevision));
                return commit;
            }
        }

        private void DetectDuplicate(CommitAttempt attempt)
        {
            if (_potentialDuplicates.Contains(new IdentityForDuplicationDetection(attempt)))
                throw new DuplicateCommitException(string.Format(Messages.DuplicateCommitIdException,
                    attempt.StreamId, attempt.BucketId, attempt.CommitId));
        }

        public IEnumerable<IStreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            lock (_commits)
            {
                return _heads
                    .Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
                    .Select(stream => new StreamHead(stream.BucketId, stream.StreamId, stream.HeadRevision,
                        stream.SnapshotRevision));
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
                var currentHead = _heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
                if (currentHead == null) return false;

                // if the snapshot is already there do NOT add it (follow the SQL implementation)
                // and the original GetSnapshot behavior which was to return the first one that was
                // added to the collection
                if (_snapshots.Any(s =>
                        s.StreamId == snapshot.StreamId && s.StreamRevision == snapshot.StreamRevision))
                    return false;

                _snapshots.Add(snapshot);
                _heads.Remove(currentHead);
                _heads.Add(new StreamHead(currentHead.BucketId, currentHead.StreamId, currentHead.HeadRevision,
                    snapshot.StreamRevision));
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
                var commits = _commits.Where(c => c.StreamId == streamId).ToArray();
                foreach (var commit in commits) _commits.Remove(commit);
                var snapshots = _snapshots.Where(s => s.StreamId == streamId).ToArray();
                foreach (var snapshot in snapshots) _snapshots.Remove(snapshot);
                var streamHead = _heads.SingleOrDefault(s => s.StreamId == streamId);
                if (streamHead != null) _heads.Remove(streamHead);
            }
        }
    }
}