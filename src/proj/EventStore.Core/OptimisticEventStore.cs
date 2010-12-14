namespace EventStore.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly ICollection<Guid> commitIdentifiers = new HashSet<Guid>();
		private readonly IPersistStreams persistence;

		public OptimisticEventStore(IPersistStreams persistence)
		{
			this.persistence = persistence;
		}

		public virtual CommittedEventStream ReadUntil(Guid streamId, long maxRevision)
		{
			long sequence = 0;
			long revision = 0;
			object snapshot = null;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetUntil(streamId, maxRevision))
			{
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
				sequence = commit.CommitSequence;
				revision = commit.StreamRevision;
				events.AddEvents(commit);
			}

			return new CommittedEventStream(
				streamId, revision, sequence, events.ToArray(), null);
		}

		public virtual void Write(CommitAttempt uncommitted)
		{
		}
	}
}