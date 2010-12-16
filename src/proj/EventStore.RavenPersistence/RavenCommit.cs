namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;

	public class RavenCommit
	{
		public string Id
		{
			get
			{
				return this.StreamId.ToString().Replace("-", string.Empty)
				       + this.CommitSequence.ToString("D8");
			}
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