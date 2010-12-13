namespace EventStore.Core
{
	using System;
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
			var commits = this.persistence.GetUntil(streamId, maxRevision);
			return null;
		}
		public CommittedEventStream ReadFrom(Guid streamId, long minRevision)
		{
			return null;
		}
		public void Write(Commit uncommitted)
		{
		}
	}
}