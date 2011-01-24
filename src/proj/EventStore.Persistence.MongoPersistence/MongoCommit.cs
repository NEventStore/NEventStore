namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;

	public class MongoCommit
	{
		private const string IdFormat = "{0}.{1}";

		public string Id
		{
			get { return IdFormat.FormatWith(this.StreamId, this.CommitSequence); }
		}
		public Guid StreamId { get; set; }
		public Guid CommitId { get; set; }
		public int StreamRevision { get; set; }
		public int CommitSequence { get; set; }
		public Dictionary<string, object> Headers { get; set; }
		public byte[] Payload { get; set; }
		public byte[] Snapshot { get; set; }

		public bool Dispatched { get; set; }

		public int MinStreamRevision { get; set; }

		public DateTime PersistedAt { get; set; }
	}
}