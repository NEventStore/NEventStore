namespace EventStore.Persistence.InMemoryPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));

        private readonly IList<Commit> commits;
        private readonly ICollection<StreamHead> heads;
        private readonly ICollection<Commit> undispatched;
        private readonly ICollection<Snapshot> snapshots;
        private readonly IDictionary<Guid, DateTime> stamps;
		private bool disposed;

        public InMemoryPersistenceEngine()
        {
            LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
            commits = new List<Commit>();
            heads = new LinkedList<StreamHead>();
            undispatched = new LinkedList<Commit>();
            snapshots = new LinkedList<Snapshot>();
            stamps = new Dictionary<Guid, DateTime>();
        }


        public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.disposed = true;
			Logger.Info(Resources.DisposingEngine);
		}
		private void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Logger.Warn(Resources.AlreadyDisposed);
			throw new ObjectDisposedException(Resources.AlreadyDisposed);
		}

		public void Initialize()
		{
			Logger.Info(Resources.InitializingEngine);
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingAllCommitsFromRevision, streamId, minRevision, maxRevision);

			lock (commits)
				return commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision).ToArray();
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingAllCommitsFromTime, start);

			var commitId = stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
			if (commitId == Guid.Empty)
				return new Commit[] { };

			var startingCommit = commits.Where(x => x.CommitId == commitId).FirstOrDefault();
			return commits.Skip(commits.IndexOf(startingCommit));
		}

		public virtual IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingAllCommitsFromToTime, start, end);

			var commitId = Stamps.Where(x => x.Value >= start && x.Value < end).Select(x => x.Key).FirstOrDefault();
			if (commitId == Guid.Empty)
				return new Commit[] { };

			var startingCommit = Commits.Where(x => x.CommitId == commitId).FirstOrDefault();
			return Commits.Skip(Commits.IndexOf(startingCommit));
		}

		public virtual void Commit(Commit attempt)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);

			lock (commits)
			{
				if (commits.Contains(attempt))
					throw new DuplicateCommitException();
				if (commits.Any(c => c.StreamId == attempt.StreamId && c.StreamRevision == attempt.StreamRevision))
					throw new ConcurrencyException();

				stamps[attempt.CommitId] = attempt.CommitStamp;
				commits.Add(attempt);

				undispatched.Add(attempt);

				var head = heads.FirstOrDefault(x => x.StreamId == attempt.StreamId);
				heads.Remove(head);

				Logger.Debug(Resources.UpdatingStreamHead, attempt.StreamId);
				var snapshotRevision = head == null ? 0 : head.SnapshotRevision;
				heads.Add(new StreamHead(attempt.StreamId, attempt.StreamRevision, snapshotRevision));
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (commits)
			{
				this.ThrowWhenDisposed();
				Logger.Debug(Resources.RetrievingUndispatchedCommits, commits.Count);
				return commits.Where(c => undispatched.Contains(c));
			}
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.MarkingAsDispatched, commit.CommitId);

			lock (commits)
				undispatched.Remove(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingStreamsToSnapshot, maxThreshold);

			lock (commits)
				return heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingSnapshotForStream, streamId, maxRevision);

			lock (commits)
				return snapshots
					.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
					.OrderByDescending(x => x.StreamRevision)
					.FirstOrDefault();
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

			lock (commits)
			{
				var currentHead = heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
				if (currentHead == null)
					return false;

				snapshots.Add(snapshot);
				heads.Remove(currentHead);
				heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
			}

			return true;
		}

		public virtual void Purge()
		{
			this.ThrowWhenDisposed();
			Logger.Warn(Resources.PurgingStore);

			lock (commits)
			{
				commits.Clear();
				snapshots.Clear();
				heads.Clear();
			}
		}
	}
}