namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;

	public class RavenCommit
	{
		private const string KeyFormat = "commits/{0}-{1:D8}";

		public string Id
		{
			get { return KeyFormat.FormatWith(this.StreamId.ToHexString(), this.CommitSequence); }
		}

		public Guid StreamId { get; set; }
		public Guid CommitId { get; set; }
		public long StreamRevision { get; set; }
		public long CommitSequence { get; set; }
		public IDictionary<string, object> Headers { get; set; }
		public ICollection<EventMessage> Events { get; set; }
		public bool Dispatched { get; set; }
	}
}