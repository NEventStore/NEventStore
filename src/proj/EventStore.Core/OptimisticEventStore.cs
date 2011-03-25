namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Dispatcher;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents, ICommitEvents
	{
		private readonly CommitTracker tracker = new CommitTracker();
		private readonly IPersistStreams persistence;
		private readonly IDispatchCommits dispatcher;
		private bool disposed;

		public OptimisticEventStore(IPersistStreams persistence, IDispatchCommits dispatcher)
		{
			this.persistence = persistence;
			this.dispatcher = dispatcher;
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
			this.dispatcher.Dispose();
			this.persistence.Dispose();
		}

		public virtual IEventStream CreateStream(Guid streamId)
		{
			return new OptimisticEventStream(streamId, this);
		}
		public virtual IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(streamId, this, minRevision, maxRevision);
		}
		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(snapshot, this, maxRevision);
		}

		IEnumerable<Commit> ICommitEvents.GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			var commits = this.persistence.GetFrom(streamId, minRevision, maxRevision);
			foreach (var commit in commits)
			{
				this.tracker.Track(commit);
				yield return commit;
			}
		}

		Commit ICommitEvents.Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return null;

			this.ThrowOnDuplicateOrConcurrentWrites(attempt);
			return this.PersistAndDispatch(attempt);
		}
		protected virtual void ThrowOnDuplicateOrConcurrentWrites(Commit attempt)
		{
			if (this.tracker.Contains(attempt))
				throw new DuplicateCommitException();

			var head = this.tracker.GetStreamHead(attempt.StreamId);
			if (head == null)
				return;

			if (head.CommitSequence >= attempt.CommitSequence)
				throw new ConcurrencyException();

			if (head.StreamRevision >= attempt.StreamRevision)
				throw new ConcurrencyException();

			if (head.CommitSequence < attempt.CommitSequence - 1)
				throw new StorageException(); // beyond the end of the stream

			if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
				throw new StorageException(); // beyond the end of the stream
		}
		protected virtual Commit PersistAndDispatch(Commit attempt)
		{
			var committed = this.persistence.Commit(attempt);
			if (committed == null)
				return null;

			this.tracker.Track(committed);
			this.dispatcher.Dispatch(committed);
			return committed;
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			// TODO: add to some kind of cache
			return this.persistence.GetSnapshot(streamId, maxRevision);
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			// TODO: update the cache here
			return this.persistence.AddSnapshot(snapshot);
		}
	}
}