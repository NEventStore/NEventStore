namespace NEventStore.Persistence.InMemoryPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Logging;

    public class InMemoryPersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (InMemoryPersistenceEngine));
        private readonly IList<Commit> _commits = new List<Commit>();
        private readonly ICollection<StreamHead> _heads = new LinkedList<StreamHead>();
        private readonly ICollection<Snapshot> _snapshots = new LinkedList<Snapshot>();
        private readonly IDictionary<Guid, DateTime> _stamps = new Dictionary<Guid, DateTime>();
        private readonly ICollection<Commit> _undispatched = new LinkedList<Commit>();
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            Logger.Info(Resources.InitializingEngine);
        }

        public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromRevision, streamId, minRevision, maxRevision);

            lock (_commits)
                return
                    _commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision)
                            .OrderBy(c => c.CommitSequence)
                            .ToArray();
        }

        public virtual IEnumerable<Commit> GetFrom(DateTime start)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromTime, start);

            Guid commitId = _stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
            if (commitId == Guid.Empty)
            {
                return new Commit[] {};
            }

            Commit startingCommit = _commits.FirstOrDefault(x => x.CommitId == commitId);
            return _commits.Skip(_commits.IndexOf(startingCommit));
        }

        public virtual IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingAllCommitsFromToTime, start, end);

            IEnumerable<Guid> selectedCommitIds = _stamps.Where(x => x.Value >= start && x.Value < end).Select(x => x.Key);
            Guid firstCommitId = selectedCommitIds.FirstOrDefault();
            Guid lastCommitId = selectedCommitIds.LastOrDefault();

            if (lastCommitId == Guid.Empty && lastCommitId == Guid.Empty)
            {
                return new Commit[] {};
            }

            Commit startingCommit = _commits.FirstOrDefault(x => x.CommitId == firstCommitId);
            Commit endingCommit = _commits.FirstOrDefault(x => x.CommitId == lastCommitId);

            int startingCommitIndex = (startingCommit == null) ? 0 : _commits.IndexOf(startingCommit);
            int endingCommitIndex = (endingCommit == null) ? _commits.Count - 1 : _commits.IndexOf(endingCommit);
            int numberToTake = endingCommitIndex - startingCommitIndex + 1;

            return _commits.Skip(_commits.IndexOf(startingCommit)).Take(numberToTake);
        }

        public virtual void Commit(Commit attempt)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);

            lock (_commits)
            {
                if (_commits.Contains(attempt))
                {
                    throw new DuplicateCommitException();
                }
                if (_commits.Any(c => c.StreamId == attempt.StreamId && c.CommitSequence == attempt.CommitSequence))
                {
                    throw new ConcurrencyException();
                }

                _stamps[attempt.CommitId] = attempt.CommitStamp;
                _commits.Add(attempt);

                _undispatched.Add(attempt);

                StreamHead head = _heads.FirstOrDefault(x => x.StreamId == attempt.StreamId);
                _heads.Remove(head);

                Logger.Debug(Resources.UpdatingStreamHead, attempt.StreamId);
                int snapshotRevision = head == null ? 0 : head.SnapshotRevision;
                _heads.Add(new StreamHead(attempt.StreamId, attempt.StreamRevision, snapshotRevision));
            }
        }

        public virtual IEnumerable<Commit> GetUndispatchedCommits()
        {
            lock (_commits)
            {
                ThrowWhenDisposed();
                Logger.Debug(Resources.RetrievingUndispatchedCommits, _commits.Count);
                return _commits.Where(c => _undispatched.Contains(c)).OrderBy(c => c.CommitSequence);
            }
        }

        public virtual void MarkCommitAsDispatched(Commit commit)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.MarkingAsDispatched, commit.CommitId);

            lock (_commits)
                _undispatched.Remove(commit);
        }

        public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingStreamsToSnapshot, maxThreshold);

            lock (_commits)
                return _heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
                             .Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
        }

        public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.GettingSnapshotForStream, streamId, maxRevision);

            lock (_commits)
                return _snapshots
                    .Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
                    .OrderByDescending(x => x.StreamRevision)
                    .FirstOrDefault();
        }

        public virtual bool AddSnapshot(Snapshot snapshot)
        {
            ThrowWhenDisposed();
            Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

            lock (_commits)
            {
                StreamHead currentHead = _heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
                if (currentHead == null)
                {
                    return false;
                }

                _snapshots.Add(snapshot);
                _heads.Remove(currentHead);
                _heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
            }

            return true;
        }

        public virtual void Purge()
        {
            ThrowWhenDisposed();
            Logger.Warn(Resources.PurgingStore);

            lock (_commits)
            {
                _commits.Clear();
                _snapshots.Clear();
                _heads.Clear();
            }
        }

        public void Drop()
        {
            Purge();
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        protected virtual void Dispose(bool disposing)
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
    }
}
