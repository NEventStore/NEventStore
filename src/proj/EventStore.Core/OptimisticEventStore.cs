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

		public OptimisticEventStore(IPersistStreams persistence, IDispatchCommits dispatcher)
		{
			this.persistence = persistence;
			this.dispatcher = dispatcher;
		}

		public virtual IEventStream CreateStream(Guid streamId)
		{
			return new OptimisticEventStream(streamId, this.persistence);
		}

		public virtual IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision)
		{
			var commits = this.persistence.GetFrom(streamId, minRevision, maxRevision);
			return new OptimisticEventStream(streamId, this, minRevision, maxRevision, commits);
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

		public virtual void Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return;

			this.ThrowOnDuplicateOrConcurrentWrites(attempt);
			this.PersistAndDispatch(attempt);
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

			if (head.CommitSequence < attempt.CommitSequence)
				throw new StorageException(); // beyond the end of the stream

			if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
				throw new StorageException(); // beyond the end of the stream
		}
		protected virtual void PersistAndDispatch(Commit attempt)
		{
			this.persistence.Commit(attempt);
			this.tracker.Track(attempt);
			this.dispatcher.Dispatch(attempt);
		}
	}
}