using System;

namespace EventStore.Persistence.RavenPersistence
{
    public class RavenStreamHead
    {
        public string Id { get; set; }
        public Guid StreamId { get; set; }
        public int HeadRevision { get; set; }
        public int SnapshotRevision { get; set; }
    }
}