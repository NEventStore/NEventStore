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
			var stream = new OptimisticEventStream(streamId, this, minRevision, maxRevision);
			return stream.CommitSequence == 0 ? null : stream;
		}

		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			return new OptimisticEventStream(snapshot, this, maxRevision);
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			var commits = this.persistence.GetFrom(streamId, minRevision, maxRevision);
			foreach (var commit in commits)
			{
				this.tracker.Track(commit);
				yield return commit;
			}
		}

		public virtual void Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return;
            try
            {
			this.ThrowOnDuplicateOrConcurrentWrites(attempt);
			this.PersistAndDispatch(attempt);
            }
            catch (ConcurrencyException e)
            {
                foreach (var commit in e.Commits)
                    this.tracker.Track(commit);
                throw;
            }
		}

		protected virtual void ThrowOnDuplicateOrConcurrentWrites(Commit attempt)
		{
			if (this.tracker.Contains(attempt))
				throw new DuplicateCommitException();

			var head = this.tracker.GetStreamHead(attempt.StreamId);
			if (head == null)
				return;

            if (head.CommitSequence >= attempt.CommitSequence)
                throw new ConcurrencyException(this.GetCommitsSinceConflictRevision(attempt));

            if (head.StreamRevision >= attempt.StreamRevision)
                throw new ConcurrencyException(this.GetCommitsSinceConflictRevision(attempt));

			if (head.CommitSequence < attempt.CommitSequence - 1)
				throw new StorageException(); // beyond the end of the stream

            if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
                throw new StorageException(); // beyond the end of the stream
        }

        protected virtual IEnumerable<Commit> GetCommitsSinceConflictRevision(Commit attempt)
        {
            var conflictRevision = attempt.StreamRevision - attempt.Events.Count + 1;
            var commits = this.persistence.GetFrom(attempt.StreamId, conflictRevision, int.MaxValue);
            return commits;
        }

        protected virtual void PersistAndDispatch(Commit attempt)
        {
            this.persistence.Commit(attempt);
			this.tracker.Track(attempt);
			this.dispatcher.Dispatch(attempt);
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			// TODO: cache
			return this.persistence.GetSnapshot(streamId, maxRevision);
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			// TODO: update cache
			return this.persistence.AddSnapshot(snapshot);
		}
	}
}