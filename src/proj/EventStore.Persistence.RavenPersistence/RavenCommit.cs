namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;

	internal class RavenCommit
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
		public List<EventMessage> Events { get; set; }
		public object Snapshot { get; set; }
	}
}