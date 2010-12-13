namespace EventStore.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly ICollection<Guid> commitIdentifiers = new HashSet<Guid>();
		private readonly IPersistStreams persistence;

		public OptimisticEventStore(IPersistStreams persistence)
		{
			this.persistence = persistence;
		}

		public CommittedEventStream ReadUntil(Guid streamId, long maxRevision)
		{
			long latestCommitSequence = 0;
			long latestStreamRevision = 0;
			object latestSnapshot = null;
			ICollection<object> events = new LinkedList<object>();

			foreach (var commit in this.persistence.GetUntil(streamId, maxRevision))
			{
				latestCommitSequence = commit.CommitSequence;

				foreach (var @event in commit.Events)
				{
					events.Add(@event.Body);
					latestStreamRevision = @event.StreamRevision;
				}

				if (commit.Snapshot == null)
					continue;

				latestSnapshot = commit.Snapshot;
				events.Clear();
			}

			return new CommittedEventStream(
				streamId, latestStreamRevision, latestCommitSequence, (ICollection)events, latestSnapshot);
		}
		public CommittedEventStream ReadFrom(Guid streamId, long minRevision)
		{
			return null;
		}
		public void Write(CommitAttempt uncommitted)
		{
		}
	}
}