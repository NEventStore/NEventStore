namespace EventStore.Persistence.InMemoryPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(InMemoryPersistenceEngine));
		private readonly IList<Commit> commits = new List<Commit>();
		private readonly ICollection<StreamHead> heads = new LinkedList<StreamHead>();
		private readonly ICollection<Commit> undispatched = new LinkedList<Commit>();
		private readonly ICollection<Snapshot> snapshots = new LinkedList<Snapshot>();
		private readonly IDictionary<Guid, DateTime> stamps = new Dictionary<Guid, DateTime>();
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

			lock (this.commits)
				return this.commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision).ToArray();
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingAllCommitsFromTime, start);

			var commitId = this.stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
			if (commitId == Guid.Empty)
				return new Commit[] { };

			var startingCommit = this.commits.FirstOrDefault(x => x.CommitId == commitId);
			return this.commits.Skip(this.commits.IndexOf(startingCommit));
		}
		public virtual void Commit(Commit attempt)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);

			lock (this.commits)
			{
				if (this.commits.Contains(attempt))
					throw new DuplicateCommitException();
				if (this.commits.Any(c => c.StreamId == attempt.StreamId && c.StreamRevision == attempt.StreamRevision))
					throw new ConcurrencyException();

				this.stamps[attempt.CommitId] = attempt.CommitStamp;
				this.commits.Add(attempt);

				this.undispatched.Add(attempt);

				var head = this.heads.FirstOrDefault(x => x.StreamId == attempt.StreamId);
				this.heads.Remove(head);

				Logger.Debug(Resources.UpdatingStreamHead, attempt.StreamId);
				var snapshotRevision = head == null ? 0 : head.SnapshotRevision;
				this.heads.Add(new StreamHead(attempt.StreamId, attempt.StreamRevision, snapshotRevision));
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (this.commits)
			{
				this.ThrowWhenDisposed();
				Logger.Debug(Resources.RetrievingUndispatchedCommits, this.commits.Count);
				return this.commits.Where(c => this.undispatched.Contains(c));
			}
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.MarkingAsDispatched, commit.CommitId);

			lock (this.commits)
				this.undispatched.Remove(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingStreamsToSnapshot, maxThreshold);

			lock (this.commits)
				return this.heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.GettingSnapshotForStream, streamId, maxRevision);

			lock (this.commits)
				return this.snapshots
					.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
					.OrderByDescending(x => x.StreamRevision)
					.FirstOrDefault();
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			this.ThrowWhenDisposed();
			Logger.Debug(Resources.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

			lock (this.commits)
			{
				var currentHead = this.heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
				if (currentHead == null)
					return false;

				this.snapshots.Add(snapshot);
				this.heads.Remove(currentHead);
				this.heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
			}

			return true;
		}

		public virtual void Purge()
		{
			this.ThrowWhenDisposed();
			Logger.Warn(Resources.PurgingStore);

			lock (this.commits)
			{
				this.commits.Clear();
				this.snapshots.Clear();
				this.heads.Clear();
			}
		}
	}
}