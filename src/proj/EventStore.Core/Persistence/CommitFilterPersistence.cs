namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CommitFilterPersistence : IPersistStreams
	{
		private readonly IPersistStreams inner;
		private readonly IEnumerable<IFilterCommitReads> readFilters;
		private readonly IEnumerable<IFilterCommitWrites> writeFilters;
		private bool disposed;

		public CommitFilterPersistence(
			IPersistStreams inner,
			IEnumerable<IFilterCommitReads> readFilter,
			IEnumerable<IFilterCommitWrites> writeFilter)
		{
			this.inner = inner;
			this.readFilters = readFilter ?? new IFilterCommitReads[0];
			this.writeFilters = writeFilter ?? new IFilterCommitWrites[0];
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

		public virtual void Initialize()
		{
			this.inner.Initialize();
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.inner.GetFrom(streamId, minRevision, maxRevision)
				.Select(this.FilterRead)
				.Where(x => x != null)
				.ToArray();
		}
		private Commit FilterRead(Commit persisted)
		{
			foreach (var filter in this.readFilters)
			{
				persisted = filter.FilterRead(persisted);
				if (persisted == null)
					break;
			}

			return persisted;
		}

		public virtual void Commit(Commit attempt)
		{
			this.inner.Commit(this.FilterWrite(attempt));
		}
		private Commit FilterWrite(Commit attempt)
		{
			foreach (var filter in this.writeFilters)
			{
				attempt = filter.FilterWrite(attempt);
				if (attempt == null)
					break;
			}

			return attempt;
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
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			return this.inner.AddSnapshot(snapshot);
		}
	}
}