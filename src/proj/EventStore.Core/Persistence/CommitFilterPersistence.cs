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

		public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return this.inner.GetUntil(streamId, maxRevision)
				.Select(this.readFilter.Filter)
				.Where(x => x != null)
				.ToArray();
		}
		public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return this.inner.GetFrom(streamId, minRevision)
				.Select(this.readFilter.Filter)
				.Where(x => x != null)
				.ToArray();
		}
		public void Persist(CommitAttempt uncommitted)
		{
			uncommitted = this.writeFilter.Filter(uncommitted);
			if (uncommitted != null)
				this.inner.Persist(uncommitted);
		}

		public IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.inner.GetUndispatchedCommits();
		}
		public void MarkCommitAsDispatched(Commit commit)
		{
			this.inner.MarkCommitAsDispatched(commit);
		}

		public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.inner.GetStreamsToSnapshot(maxThreshold);
		}
		public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
		{
			this.inner.AddSnapshot(streamId, streamRevision, snapshot);
		}
	}
}