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
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (InMemoryPersistenceEngine));
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
            Logger.Info(Resources.InitializingEngine);
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromRevision, streamId, minRevision, maxRevision);
            return this[bucketId].GetFrom(streamId, minRevision, maxRevision);
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromTime, bucketId, start);
            return this[bucketId].GetFrom(start);
        }

        public IEnumerable<ICommit> GetFrom(ICheckpoint checkpoint)
        {
            Logger.Debug(Resources.GettingAllCommitsFromCheckpoint, checkpoint);
            return _buckets
                .Values
                .SelectMany(b => b.GetCommits())
                .Where(c => c.Checkpoint.CompareTo(checkpoint) > 0)
                .OrderBy(c => c.Checkpoint)
                .ToArray();
        }

        public ICheckpoint StartCheckpoint { get { return new IntCheckpoint(0); } }

        public ICheckpoint ParseCheckpoint(string checkpointValue)
        {
            return IntCheckpoint.Parse(checkpointValue);
        }

        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromToTime, start, end);
            return this[bucketId].GetFromTo(start, end);
        }

        public ICommit Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);
            return this[attempt.BucketId].Commit(attempt, new IntCheckpoint(Interlocked.Increment(ref _checkpoint)));
        }

        public IEnumerable<ICommit> GetUndispatchedCommits()
        {
            ThrowWhenDisposed();
            return _buckets.Values.SelectMany(b => b.GetUndispatchedCommits());
        }

        public void MarkCommitAsDispatched(ICommit commit)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.MarkingAsDispatched, commit.CommitId);
            this[commit.BucketId].MarkCommitAsDispatched(commit);
        }

        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingStreamsToSnapshot, bucketId, maxThreshold);
            return this[bucketId].GetStreamsToSnapshot(maxThreshold);
        }

        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingSnapshotForStream, bucketId, streamId, maxRevision);
            return this[bucketId].GetSnapshot(streamId, maxRevision);
        }

        public bool AddSnapshot(ISnapshot snapshot)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);
            return this[snapshot.BucketId].AddSnapshot(snapshot);
        }

        public void Purge()
        {
            ThrowWhenDisposed();
            Logger.Warn(Resources.PurgingStore);
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
            Logger.Warn(Resources.DeletingStream, streamId, bucketId);
            Bucket bucket;
            if (!_buckets.TryGetValue(bucketId, out bucket))
            {
                return;
            }
            bucket.DeleteStream(streamId);
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        private void Dispose(bool disposing)
        {
            _disposed = true;
            Logger.Info(Resources.DisposingEngine);
        }

        private void ThrowWhenDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            Logger.Warn(Resources.AlreadyDisposed);
            throw new ObjectDisposedException(Resources.AlreadyDisposed);
        }

        private class Bucket
        {
            private readonly IList<ICommit> _commits = new List<ICommit>();

            public IEnumerable<ICommit> GetCommits()
            {
                lock (_commits)
                {
                    return _commits.ToArray();
                }
            }

            private readonly ICollection<IStreamHead> _heads = new LinkedList<IStreamHead>();
            private readonly ICollection<ISnapshot> _snapshots = new LinkedList<ISnapshot>();
            private readonly IDictionary<Guid, DateTime> _stamps = new Dictionary<Guid, DateTime>();
            private readonly ICollection<ICommit> _undispatched = new LinkedList<ICommit>();

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

                ICommit startingCommit = _commits.FirstOrDefault(x => x.CommitId == commitId);
                return _commits.Skip(_commits.IndexOf(startingCommit));
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
                ICommit startingCommit = _commits.FirstOrDefault(x => x.CommitId == firstCommitId);
                ICommit endingCommit = _commits.FirstOrDefault(x => x.CommitId == lastCommitId);
                int startingCommitIndex = (startingCommit == null) ? 0 : _commits.IndexOf(startingCommit);
                int endingCommitIndex = (endingCommit == null) ? _commits.Count - 1 : _commits.IndexOf(endingCommit);
                int numberToTake = endingCommitIndex - startingCommitIndex + 1;

                return _commits.Skip(_commits.IndexOf(startingCommit)).Take(numberToTake);
            }

            public ICommit Commit(CommitAttempt attempt, ICheckpoint checkpoint)
            {
                lock (_commits)
                {
                    DetectDuplicate(attempt);
                    ICommit commit = attempt.ToCommit(checkpoint);
                    if (_commits.Any(c => c.StreamId == commit.StreamId && c.CommitSequence == commit.CommitSequence))
                    {
                        throw new ConcurrencyException();
                    }
                    _stamps[commit.CommitId] = commit.CommitStamp;
                    _commits.Add(commit);
                    _undispatched.Add(commit);
                    IStreamHead head = _heads.FirstOrDefault(x => x.StreamId == commit.StreamId);
                    _heads.Remove(head);
                    Logger.Debug(Resources.UpdatingStreamHead, commit.StreamId);
                    int snapshotRevision = head == null ? 0 : head.SnapshotRevision;
                    _heads.Add(new StreamHead(commit.BucketId, commit.StreamId, commit.StreamRevision, snapshotRevision));
                    return commit;
                }
            }

            private void DetectDuplicate(CommitAttempt attempt)
            {
                if (_commits.Any(c => 
                    c.BucketId == attempt.BucketId &&
                    c.StreamId == attempt.StreamId &&
                    c.CommitId == attempt.CommitId &&
                    c.CommitSequence == attempt.CommitSequence))
                {
                    throw new DuplicateCommitException();
                }
            }

            public IEnumerable<ICommit> GetUndispatchedCommits()
            {
                lock (_commits)
                {
                    Logger.Debug(Resources.RetrievingUndispatchedCommits, _commits.Count);
                    return _commits.Where(c => _undispatched.Contains(c)).OrderBy(c => c.CommitSequence);
                }
            }

            public void MarkCommitAsDispatched(ICommit commit)
            {
                lock (_commits)
                {
                    _undispatched.Remove(commit);
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
                }
            }

            public void DeleteStream(string streamId)
            {
                lock (_commits)
                {
                    ICommit[] commits = _commits.Where(c => c.StreamId == streamId).ToArray();
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