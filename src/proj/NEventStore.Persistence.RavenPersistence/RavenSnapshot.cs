namespace NEventStore.Persistence.RavenPersistence
{
    using System;

    public class RavenSnapshot
	{
        public string Id { get; set; }
        public string Partition { get; set; }
		public Guid StreamId { get; set; }
		public int StreamRevision { get; set; }
		public object Payload { get; set; }
	}
}