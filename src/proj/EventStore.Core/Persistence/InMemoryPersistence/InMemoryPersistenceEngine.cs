namespace EventStore.Persistence.InMemoryPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
		private static readonly IList<Commit> Commits = new List<Commit>();
		private static readonly ICollection<StreamHead> Heads = new LinkedList<StreamHead>();
		private static readonly ICollection<Commit> Undispatched = new LinkedList<Commit>();
		private static readonly ICollection<Snapshot> Snapshots = new LinkedList<Snapshot>();
		private static readonly IDictionary<Guid, DateTime> Stamps = new Dictionary<Guid, DateTime>();
		private bool disposed;

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

			lock (Commits)
				return Commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision).ToArray();
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingAllCommitsFromTime, start);

			var commitId = Stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
			if (commitId == Guid.Empty)
				return new Commit[] { };

			var startingCommit = Commits.Where(x => x.CommitId == commitId).FirstOrDefault();
			return Commits.Skip(Commits.IndexOf(startingCommit));
		}
		public virtual void Commit(Commit attempt)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);

			lock (Commits)
			{
				if (Commits.Contains(attempt))
					throw new DuplicateCommitException();
				if (Commits.Any(c => c.StreamId == attempt.StreamId && c.StreamRevision == attempt.StreamRevision))
					throw new ConcurrencyException();

				Stamps[attempt.CommitId] = attempt.CommitStamp;
				Commits.Add(attempt);

				Undispatched.Add(attempt);

				var head = Heads.FirstOrDefault(x => x.StreamId == attempt.StreamId);
				Heads.Remove(head);

				Logger.Debug(Resources.UpdatingStreamHead, attempt.StreamId);
				var snapshotRevision = head == null ? 0 : head.SnapshotRevision;
				Heads.Add(new StreamHead(attempt.StreamId, attempt.StreamRevision, snapshotRevision));
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (Commits)
			{
				this.ThrowWhenDisposed();
				Logger.Debug(Resources.RetrievingUndispatchedCommits, Commits.Count);
				return Commits.Where(c => Undispatched.Contains(c));
			}
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.MarkingAsDispatched, commit.CommitId);

			lock (Commits)
				Undispatched.Remove(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingStreamsToSnapshot, maxThreshold);

			lock (Commits)
				return Heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingSnapshotForStream, streamId, maxRevision);

			lock (Commits)
				return Snapshots
					.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
					.OrderByDescending(x => x.StreamRevision)
					.FirstOrDefault();
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

			lock (Commits)
			{
				var currentHead = Heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
				if (currentHead == null)
					return false;

				Snapshots.Add(snapshot);
				Heads.Remove(currentHead);
				Heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
			}

			return true;
		}

		public virtual void Purge()
		{
			this.ThrowWhenDisposed();
			Logger.Warn(Resources.PurgingStore);

			lock (Commits)
			{
				Commits.Clear();
				Snapshots.Clear();
				Heads.Clear();
			}
		}
	}
}