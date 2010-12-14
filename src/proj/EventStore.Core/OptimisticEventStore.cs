namespace EventStore.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Dispatcher;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IDictionary<Guid, Commit> latest = new Dictionary<Guid, Commit>();
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
			Commit mostRecent = null;
			long sequence = 0;
			long revision = 0;
			object snapshot = null;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetUntil(streamId, maxRevision))
			{
				this.commitIdentifiers.Add(commit.CommitId);
				mostRecent = commit;
				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;
				snapshot = commit.Snapshot ?? snapshot;
				events.AddEventsOrClearOnSnapshot(commit);
			}

			if (mostRecent != null)
				this.latest[streamId] = mostRecent;

			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), snapshot);
		}

		public virtual CommittedEventStream ReadFrom(Guid streamId, long minRevision)
		{
			Commit mostRecent = null;
			long sequence = 0;
			long revision = 0;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetFrom(streamId, minRevision))
			{
				this.commitIdentifiers.Add(commit.CommitId);
				mostRecent = commit;
				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;
				events.AddEvents(commit);
			}

			if (mostRecent != null)
				this.latest[streamId] = mostRecent;

			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), null);
		}

		public virtual void Write(CommitAttempt attempt)
		{
			if (!attempt.IsValid() || !attempt.HasEvents())
				return;

			if (this.commitIdentifiers.Contains(attempt.CommitId))
				throw new DuplicateCommitException();

			Commit previous;
			if (this.latest.TryGetValue(attempt.StreamId, out previous) && previous.CommitSequence >= attempt.CommitSequence)
				throw new ConcurrencyException();

			if (previous != null && previous.StreamRevision >= attempt.StreamRevision)
				throw new ConcurrencyException();

			this.persistence.Persist(attempt);
			var commit = this.Dispatch(attempt);

			this.commitIdentifiers.Add(commit.CommitId);
			this.latest[attempt.StreamId] = commit;
		}
		private Commit Dispatch(CommitAttempt attempt)
		{
			var commit = attempt.ToCommit();
			this.dispatcher.Dispatch(commit);
			return commit;
		}
	}
}