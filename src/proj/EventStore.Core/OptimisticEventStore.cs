namespace EventStore.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Dispatcher;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IDictionary<Guid, Commit> streamHeads = new Dictionary<Guid, Commit>();
		private readonly ICollection<Guid> commitIdentifiers = new HashSet<Guid>();
		private readonly IPersistStreams persistence;
		private readonly IDispatchCommits dispatcher;

		public OptimisticEventStore(IPersistStreams persistence, IDispatchCommits dispatcher)
		{
			this.persistence = persistence;
			this.dispatcher = dispatcher;
		}

		public virtual CommittedEventStream ReadUntil(Guid streamId, long maxRevision)
		{
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

			foreach (var commit in commits)
			{
				streamId = commit.StreamId;
				last = commit;
				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;

				snapshot = commit.Snapshot ?? snapshot;
				events.AddEventsOrClearOnSnapshot(commit, applySnapshot);

				this.commitIdentifiers.Add(commit.CommitId);
			}

			if (last != null)
				this.streamHeads[streamId] = last;

			snapshot = applySnapshot ? snapshot : null;
			return new CommittedEventStream(streamId, revision, sequence, events.ToArray(), snapshot);
		}

		public virtual void Write(CommitAttempt attempt)
		{
			if (!attempt.IsValid() || !attempt.IsEmpty())
				return;

			this.ThrowOnDuplicateOrConcurrentWrites(attempt);
			this.PersistAndDispatch(attempt);
		}
		protected virtual void ThrowOnDuplicateOrConcurrentWrites(CommitAttempt attempt)
		{
			if (this.commitIdentifiers.Contains(attempt.CommitId))
				throw new DuplicateCommitException();

			Commit previousCommitForStream;
			if (!this.streamHeads.TryGetValue(attempt.StreamId, out previousCommitForStream))
				return;

			if (previousCommitForStream.CommitSequence > attempt.PreviousCommitSequence)
				throw new ConcurrencyException();

			if (previousCommitForStream.StreamRevision > attempt.PreviousStreamRevision)
				throw new ConcurrencyException();

			if (previousCommitForStream.CommitSequence < attempt.PreviousCommitSequence)
				throw new PersistenceException();

			if (previousCommitForStream.StreamRevision < attempt.PreviousStreamRevision)
				throw new PersistenceException();
		}
		protected virtual void PersistAndDispatch(CommitAttempt attempt)
		{
			this.persistence.Persist(attempt);

			var commit = attempt.ToCommit();
			this.commitIdentifiers.Add(commit.CommitId);
			this.streamHeads[commit.StreamId] = commit;
			
			this.dispatcher.Dispatch(commit);
		}
	}
}