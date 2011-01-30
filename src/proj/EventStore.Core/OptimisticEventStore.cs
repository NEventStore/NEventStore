namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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
			return this.OpenStream(streamId, minRevision, maxRevision, commits);
		}
		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			// we query from the revision of the snapshot forward because we are guaranteed to get
			// a commit.  If we queried beyond the snapshot (snapshot.StreamRevision + 1), we cannot be
			// sure that there's anything out there.  This would result in an empty string that had
			// a CommitSequence and StreamRevision of 0 which could never be properly persisted.
			var streamId = snapshot.StreamId;
			var minRevision = snapshot.StreamRevision;

			var commits = this.persistence.GetFrom(streamId, minRevision, maxRevision);
			return this.OpenStream(streamId, minRevision + 1, maxRevision, commits)
				?? new OptimisticEventStream(
					streamId, this.persistence, minRevision, commits.First().CommitSequence);
		}
		private IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision, IEnumerable<Commit> commits)
		{
			var stream = new OptimisticEventStream(streamId, this, minRevision, maxRevision, commits);
			return stream.CommitSequence == 0 ? null : stream;
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