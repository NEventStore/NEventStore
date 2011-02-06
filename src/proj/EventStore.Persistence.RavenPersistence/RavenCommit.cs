namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;

	public class RavenCommit
	{
		public string Id { get; set; }

		public Guid StreamId { get; set; }
		public int CommitSequence { get; set; }

		public int StartingStreamRevision { get; set; }
		public int StreamRevision { get; set; }

		public Guid CommitId { get; set; }
		public DateTime CommitStamp { get; set; }

		public Dictionary<string, object> Headers { get; set; }
		public byte[] Payload { get; set; }

		public bool Dispatched { get; set; }
	}
}