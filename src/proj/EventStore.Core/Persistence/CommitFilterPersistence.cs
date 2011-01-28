namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CommitFilterPersistence : IPersistStreams
	{
		private readonly IPersistStreams inner;
		private readonly IFilterCommits<Commit> readFilter;
		private readonly IFilterCommits<Commit> writeFilter;
		private bool disposed;

		public CommitFilterPersistence(
			IPersistStreams inner, IFilterCommits<Commit> readFilter, IFilterCommits<Commit> writeFilter)
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

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.inner.GetFrom(streamId, minRevision, maxRevision)
				.Select(this.readFilter.Filter)
				.Where(x => x != null)
				.ToArray();
		}
		public virtual void Commit(Commit attempt)
		{
			attempt = this.writeFilter.Filter(attempt);
			if (attempt != null)
				this.inner.Commit(attempt);
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
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.inner.GetSnapshot(streamId, maxRevision);
		}
		public virtual void AddSnapshot(Snapshot snapshot)
		{
			this.inner.AddSnapshot(snapshot);
		}
	}
}