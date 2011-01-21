namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Dispatcher;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly CommitTracker tracker = new CommitTracker();
		private readonly IPersistStreams persistence;
		private readonly IDispatchCommits dispatcher;

		public OptimisticEventStore(IPersistStreams persistence, IDispatchCommits dispatcher)
		{
			this.persistence = persistence;
			this.dispatcher = dispatcher;
		}

		public virtual CommittedEventStream ReadFromSnapshotUntil(Guid streamId, int maxRevision)
		{
			maxRevision = maxRevision > 0 ? maxRevision : int.MaxValue;
			return this.Read(this.persistence.GetFromSnapshotUntil(streamId, maxRevision), true);
		}
		public virtual CommittedEventStream ReadFrom(Guid streamId, int minRevision)
		{
			return this.Read(this.persistence.GetFrom(streamId, minRevision), false);
		}
		protected virtual CommittedEventStream Read(IEnumerable<Commit> commits, bool applySnapshot)
		{
			var streamId = Guid.Empty;
			var revision = 0;
			var sequence = 0;
			object snapshot = null;
			ICollection<object> events = new LinkedList<object>();
			ICollection<Guid> commitIdentifiers = new HashSet<Guid>();

			foreach (var commit in commits ?? new Commit[0])
			{
				this.tracker.Track(commit);
				commitIdentifiers.Add(commit.CommitId);

				streamId = commit.StreamId;
				revision = commit.StreamRevision;
				sequence = commit.CommitSequence;
				snapshot = commit.Snapshot ?? snapshot;

				events.AddEventsOrClearOnSnapshot(commit, applySnapshot);
			}

			snapshot = applySnapshot ? snapshot : null;
			return new CommittedEventStream(streamId, revision, sequence, events, commitIdentifiers, snapshot);
		}

		public virtual void Write(CommitAttempt attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return;

			this.ThrowOnDuplicateOrConcurrentWrites(attempt);
			this.PersistAndDispatch(attempt);
		}
		protected virtual void ThrowOnDuplicateOrConcurrentWrites(CommitAttempt current)
		{
			if (this.tracker.Contains(current))
				throw new DuplicateCommitException();

			var previous = this.tracker.GetStreamHead(current.StreamId);
			if (previous == null)
				return;

			if (previous.CommitSequence > current.PreviousCommitSequence)
				throw new ConcurrencyException();

			if (previous.StreamRevision >= current.StreamRevision)
				throw new ConcurrencyException();

			if (previous.CommitSequence < current.PreviousCommitSequence)
				throw new PersistenceEngineException(); // beyond the end of the stream

			if (previous.StreamRevision < current.StreamRevision - current.Events.Count)
				throw new PersistenceEngineException(); // beyond the end of the stream
		}
		protected virtual void PersistAndDispatch(CommitAttempt attempt)
		{
			this.persistence.Persist(attempt);

			var commit = attempt.ToCommit();
			this.tracker.Track(commit);
			
			this.dispatcher.Dispatch(commit);
		}
	}
}