namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CommitFilterPersistence : IPersistStreams
	{
		private readonly IPersistStreams inner;
		private readonly IEnumerable<IFilterCommits<Commit>> readFilters;
		private readonly IEnumerable<IFilterCommits<Commit>> writeFilters;
		private bool disposed;

		public CommitFilterPersistence(
			IPersistStreams inner,
			IEnumerable<IFilterCommits<Commit>> readFilter,
			IEnumerable<IFilterCommits<Commit>> writeFilter)
		{
			this.inner = inner;
			this.readFilters = readFilter ?? new IFilterCommits<Commit>[0];
			this.writeFilters = writeFilter ?? new IFilterCommits<Commit>[0];
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
				.Select(commit => Filter(commit, this.readFilters))
				.Where(x => x != null)
				.ToArray();
		}
		public virtual void Commit(Commit attempt)
		{
			attempt = Filter(attempt, this.writeFilters);
			this.inner.Commit(attempt);
		}
		private static Commit Filter(Commit attempt, IEnumerable<IFilterCommits<Commit>> filters)
		{
			foreach (var filter in filters)
			{
				attempt = filter.Filter(attempt);
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