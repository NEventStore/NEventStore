namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Dispatcher;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IDictionary<Guid, Commit> streamHeads = new Dictionary<Guid, Commit>();
		private readonly CommitTracker tracker = new CommitTracker();
		private readonly IPersistStreams persistence;
		private readonly IDispatchCommits dispatcher;

		public OptimisticEventStore(IPersistStreams persistence, IDispatchCommits dispatcher)
		{
			this.persistence = persistence;
			this.dispatcher = dispatcher;
		}

		public virtual CommittedEventStream ReadUntil(Guid streamId, long maxRevision)
		{
			maxRevision = maxRevision > 0 ? maxRevision : long.MaxValue;
			return this.Read(this.persistence.GetUntil(streamId, maxRevision), true);
		}
		public virtual CommittedEventStream ReadFrom(Guid streamId, long minRevision)
		{
			return this.Read(this.persistence.GetFrom(streamId, minRevision), false);
		}
		protected virtual CommittedEventStream Read(IEnumerable<Commit> commits, bool applySnapshot)
		{
			var streamId = Guid.Empty;
			Commit last = null;
			long sequence = 0;
			long revision = 0;
			object snapshot = null;
			ICollection<object> events = new LinkedList<object>();
			ICollection<Guid> commitIdentifiers = new HashSet<Guid>();

			foreach (var commit in commits ?? new Commit[0])
			{
				streamId = commit.StreamId;
				last = commit;
				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;

				snapshot = commit.Snapshot ?? snapshot;
				events.AddEventsOrClearOnSnapshot(commit, applySnapshot);
				commitIdentifiers.Add(commit.CommitId);

				this.tracker.Track(commit);
			}

			if (last != null)
				this.streamHeads[streamId] = last;

			snapshot = applySnapshot ? snapshot : null;
			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), commitIdentifiers, snapshot);
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

			Commit previous;
			if (!this.streamHeads.TryGetValue(current.StreamId, out previous))
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
			this.streamHeads[commit.StreamId] = commit;
			
			this.dispatcher.Dispatch(commit);
		}
	}
}