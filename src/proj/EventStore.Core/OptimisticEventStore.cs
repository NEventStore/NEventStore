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
			long sequence = 0;
			long revision = 0;
			object snapshot = null;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetUntil(streamId, maxRevision))
			{
				this.commitIdentifiers.Add(commit.CommitId);

				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;
				snapshot = commit.Snapshot ?? snapshot;
				events.AddEventsOrClearOnSnapshot(commit);
			}

			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), snapshot);
		}

		public virtual CommittedEventStream ReadFrom(Guid streamId, long minRevision)
		{
			long sequence = 0;
			long revision = 0;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetFrom(streamId, minRevision))
			{
				this.commitIdentifiers.Add(commit.CommitId);

				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;
				events.AddEvents(commit);
			}

			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), null);
		}

		public virtual void Write(CommitAttempt attempt)
		{
			if (!attempt.IsValid() || !attempt.HasEvents())
				return;

			if (this.commitIdentifiers.Contains(attempt.CommitId))
				throw new DuplicateCommitException();

			this.persistence.Persist(attempt);
			var commit = this.Dispatch(attempt);

			//// TODO:
			//// this.commitIdentifiers.Add(commit.CommitId);
			//// this.latest[attempt.CommitId] = commit;
		}
		private Commit Dispatch(CommitAttempt attempt)
		{
			var commit = attempt.ToCommit();
			this.dispatcher.Dispatch(commit);
			return commit;
		}
	}
}