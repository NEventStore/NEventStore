namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;

	public class RavenCommit
	{
		public string Id { get; set; }

		public Guid StreamId { get; set; }
		public int CommitSequence { get; set; }

		public int StartingStreamRevision { get; set; }
		public int StreamRevision { get; set; }

		public Guid CommitId { get; set; }
		public DateTime CommitStamp { get; set; }

		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
			Justification = "This is a simple DTO and is only used internally by Raven.")]
		public Dictionary<string, object> Headers { get; set; }
		public object Payload { get; set; }

		public bool Dispatched { get; set; }
	}
}