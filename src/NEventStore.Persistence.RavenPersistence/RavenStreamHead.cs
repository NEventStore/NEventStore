namespace NEventStore.Persistence.RavenPersistence
{
    public class RavenStreamHead
    {
        public string Id { get; set; }
        public string Partition { get; set; }
        public string BucketId { get; set; }
        public string StreamId { get; set; }
        public int HeadRevision { get; set; }
        public int SnapshotRevision { get; set; }

        public int SnapshotAge
        {
            get { return HeadRevision - SnapshotRevision; } // set by map/reduce on the server
        }
    }
}