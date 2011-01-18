namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
		private readonly ICollection<Commit> commits = new LinkedList<Commit>();
		private readonly ICollection<StreamHead> heads = new LinkedList<StreamHead>();
		private readonly ICollection<Commit> undispatchedCommits = new LinkedList<Commit>();
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

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, int maxRevision)
		{
			lock (this.commits)
			{
				var snapshotCommit = this.commits
					.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Snapshot != null)
					.OrderByDescending(o => o.StreamRevision)
					.Take(1)
					.FirstOrDefault();

				int snapshotRevision = 0;
				if (snapshotCommit != null)
					snapshotRevision = snapshotCommit.StreamRevision;

				return this.commits
					.Where(x => x.StreamId == streamId && x.StreamRevision >= snapshotRevision && x.StreamRevision <= maxRevision)
					.ToList();
			}
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision)
		{
			lock (this.commits)
				return this.commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			lock (this.commits)
			{
				var commit = uncommitted.ToCommit();

				if (this.commits.Contains(commit))
					throw new DuplicateCommitException();
				if (this.commits.Any(c => c.StreamId == commit.StreamId && c.StreamRevision == commit.StreamRevision))
					throw new ConcurrencyException();

				this.stamps[commit.CommitId] = DateTime.UtcNow;
				this.commits.Add(commit);

				lock (this.undispatchedCommits)
					this.undispatchedCommits.Add(commit);

				lock (this.heads)
				{
					var head = new StreamHead(commit.StreamId, commit.StreamRevision, 0);
					if (this.heads.Contains(head))
						this.heads.Remove(head);
					this.heads.Add(head);
				}
			}
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			var commitId = this.stamps.Where(x => x.Value >= start).Select(x => x.Key).FirstOrDefault();

			var found = false;
			return this.commits.TakeWhile(x =>
			{
				if (!found && commitId == x.CommitId)
					found = true;

				return found;
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (this.commits)
				return this.commits.Where(c => this.undispatchedCommits.Contains(c));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			lock (this.undispatchedCommits)
				this.undispatchedCommits.Remove(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			lock (this.heads)
				return this.heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual void AddSnapshot(Guid streamId, int streamRevision, object snapshot)
		{
			lock (this.commits)
			{
				var commitToBeUpdated =
					this.commits.First(commit => commit.StreamId == streamId && commit.StreamRevision == streamRevision);

				this.commits.Remove(commitToBeUpdated);
				this.commits.Add(new Commit(commitToBeUpdated.StreamId,
					commitToBeUpdated.StreamRevision,
					commitToBeUpdated.CommitId,
					commitToBeUpdated.CommitSequence,
					commitToBeUpdated.Headers,
					commitToBeUpdated.Events,
					snapshot));
			}
			lock (this.heads)
			{
				var currentHead = this.heads.First(h => h.StreamId == streamId);

				this.heads.Remove(currentHead);
				this.heads.Add(new StreamHead(currentHead.StreamId, currentHead.HeadRevision, streamRevision));
			}
		}
	}
}