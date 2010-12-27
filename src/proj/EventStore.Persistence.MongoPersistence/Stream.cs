namespace EventStore.Persistence.MongoPersistence
{
    using System;

    public class Stream
    {
        public Guid StreamId { get; set; }
        public long HeadRevision { get; set; }
        public long SnapshotRevision { get; set; }
    }
}