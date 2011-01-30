namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
		private readonly IList<Commit> commits = new List<Commit>();
		private readonly ICollection<StreamHead> heads = new LinkedList<StreamHead>();
		private readonly ICollection<Commit> undispatched = new LinkedList<Commit>();
		private readonly ICollection<Snapshot> snapshots = new LinkedList<Snapshot>();
		private readonly IDictionary<Guid, DateTime> stamps = new Dictionary<Guid, DateTime>();

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}

		public void Initialize()
		{
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			lock (this.commits)
				return this.commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision && (x.StreamRevision - x.Events.Count + 1) <= maxRevision).ToArray();
		}
		public virtual void Commit(Commit attempt)
		{
			lock (this.commits)
			{
				if (this.commits.Contains(attempt))
					throw new DuplicateCommitException();
				if (this.commits.Any(c => c.StreamId == attempt.StreamId && c.StreamRevision == attempt.StreamRevision))
					throw new ConcurrencyException();

				this.stamps[attempt.CommitId] = DateTime.UtcNow;
				this.commits.Add(attempt);

				lock (this.undispatched)
					this.undispatched.Add(attempt);

				lock (this.heads)
				{
					var head = new StreamHead(attempt.StreamId, attempt.StreamRevision, 0);
					if (this.heads.Contains(head))
						this.heads.Remove(head);
					this.heads.Add(head);
				}
			}
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			var commitId = this.stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();
			if (commitId == Guid.Empty)
				return new Commit[] { };

			var startingCommit = this.commits.Where(x => x.CommitId == commitId).First();
			return this.commits.Skip(this.commits.IndexOf(startingCommit));
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (this.commits)
				return this.commits.Where(c => this.undispatched.Contains(c));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			lock (this.undispatched)
				this.undispatched.Remove(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			lock (this.heads)
				return this.heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			lock (this.snapshots)
				return this.snapshots
					.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
					.OrderByDescending(x => x.StreamRevision)
					.FirstOrDefault();
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			lock (this.snapshots)
				this.snapshots.Add(snapshot);

			lock (this.heads)
			{
				var currentHead = this.heads.FirstOrDefault(h => h.StreamId == snapshot.StreamId);
				if (currentHead != null)
				{
					this.heads.Remove(currentHead);
					this.heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, snapshot.StreamRevision));
				}
			}

			return true;
		}
	}
}