using System;

namespace EventStore.Persistence.RavenPersistence
{
    public class RavenSnapshot
    {
        public Guid StreamId { get; set; }
        public int StreamRevision { get; set; }
        public byte[] Payload { get; set; }
    }
}