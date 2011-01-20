namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CommitFilterPersistence : IPersistStreams
	{
		private readonly IPersistStreams inner;
		private readonly IFilterCommits<Commit> readFilter;
		private readonly IFilterCommits<CommitAttempt> writeFilter;
		private bool disposed;

		public CommitFilterPersistence(
			IPersistStreams inner, IFilterCommits<Commit> readFilter, IFilterCommits<CommitAttempt> writeFilter)
		{
			this.inner = inner;
			this.readFilter = readFilter;
			this.writeFilter = writeFilter;
		}

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
			this.inner.Dispose();
		}

		public void Initialize()
		{
			this.inner.Initialize();
		}

		public virtual IEnumerable<Commit> GetFromSnapshotUntil(Guid streamId, int maxRevision)
		{
			return this.inner.GetFromSnapshotUntil(streamId, maxRevision)
				.Select(this.readFilter.Filter)
				.Where(x => x != null)
				.ToArray();
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision)
		{
			return this.inner.GetFrom(streamId, minRevision)
				.Select(this.readFilter.Filter)
				.Where(x => x != null)
				.ToArray();
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			uncommitted = this.writeFilter.Filter(uncommitted);
			if (uncommitted != null)
				this.inner.Persist(uncommitted);
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.inner.GetFrom(start);
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.inner.GetUndispatchedCommits();
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.inner.MarkCommitAsDispatched(commit);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.inner.GetStreamsToSnapshot(maxThreshold);
		}
		public virtual void AddSnapshot(Guid streamId, int streamRevision, object snapshot)
		{
			this.inner.AddSnapshot(streamId, streamRevision, snapshot);
		}
	}
}