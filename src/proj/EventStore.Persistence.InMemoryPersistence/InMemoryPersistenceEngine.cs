namespace EventStore.Persistence.InMemoryPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class InMemoryPersistenceEngine : IPersistStreams
	{
		private bool disposed;
		private ICollection<Commit> commits;
		private ICollection<StreamHead> heads;
		private ICollection<Commit> undispatchedCommits;

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
		}

		public void Initialize()
		{
			this.commits = new LinkedList<Commit>();
			this.undispatchedCommits = new LinkedList<Commit>();
			this.heads = new LinkedList<StreamHead>();
		}

		public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			lock (this.commits)
			{
				var snapshotCommit =
					this.commits.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Snapshot != null)
						.OrderByDescending(o => o.StreamRevision)
						.Take(1)
						.FirstOrDefault();

				long snapshotRevision = 0;
				if (snapshotCommit != null)
					snapshotRevision = snapshotCommit.StreamRevision;

				return
					this.commits.Where(
						x => x.StreamId == streamId && x.StreamRevision >= snapshotRevision && x.StreamRevision <= maxRevision)
						.ToList();
			}
		}

		public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			lock (this.commits)
				return this.commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();
		}

		public void Persist(CommitAttempt uncommitted)
		{
			lock (this.commits)
			{
				var commit = uncommitted.ToCommit();

				if (this.commits.Contains(commit))
					throw new DuplicateCommitException();
				if (this.commits.Any(c => c.StreamId == commit.StreamId && c.StreamRevision == commit.StreamRevision))
					throw new ConcurrencyException();

				this.commits.Add(commit);

				lock (this.undispatchedCommits)
					this.undispatchedCommits.Add(commit);

				lock (this.heads)
				{
					var head = new StreamHead(commit.StreamId, null, commit.StreamRevision, 0);
					if (this.heads.Contains(head))
						this.heads.Remove(head);
					this.heads.Add(head);
				}
			}
		}

		public IEnumerable<Commit> GetUndispatchedCommits()
		{
			lock (this.commits)
				return this.commits.Where(c => this.undispatchedCommits.Contains(c));
		}

		public void MarkCommitAsDispatched(Commit commit)
		{
			lock (this.undispatchedCommits)
				this.undispatchedCommits.Remove(commit);
		}

		public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			lock (this.heads)
				return this.heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
					.Select(stream => new StreamHead(stream.StreamId,
					                  	stream.StreamName,
					                  	stream.HeadRevision,
					                  	stream.SnapshotRevision));
		}

		public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
		{
			lock (this.commits)
			{
				var commitToBeUpdated =
					this.commits.First(commit => commit.StreamId == streamId && commit.StreamRevision == streamRevision);

				this.commits.Remove(commitToBeUpdated);
				this.commits.Add(new Commit(commitToBeUpdated.StreamId,
					commitToBeUpdated.CommitId,
					commitToBeUpdated.StreamRevision,
					commitToBeUpdated.CommitSequence,
					commitToBeUpdated.Headers,
					commitToBeUpdated.Events,
					snapshot));
			}
			lock (this.heads)
			{
				var currentHead = this.heads.First(h => h.StreamId == streamId);

				this.heads.Remove(currentHead);
				this.heads.Add(new StreamHead(currentHead.StreamId, currentHead.StreamName, currentHead.HeadRevision, streamRevision));
			}
		}
	}
}