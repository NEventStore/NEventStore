namespace EventStore.Persistence.RavenPersistence
{
	using System;

	public class RavenSnapshot
	{
		public Guid StreamId { get; set; }
		public int StreamRevision { get; set; }
		public object Payload { get; set; }
	}
}