namespace EventStore.Persistence.MongoDBPersistence
{
	using System;
	using System.Collections.Generic;

	public class MongoDBCommit
	{
		public MongoDBCommitId Id { get; set; }

		public int StartingStreamRevision { get; set; }
		public int StreamRevision { get; set; }

		public Guid CommitId { get; set; }
		public DateTime CommitStamp { get; set; }

		public Dictionary<string, object> Headers { get; set; }
		public byte[] Payload { get; set; }

		public bool Dispatched { get; set; }
	}
}