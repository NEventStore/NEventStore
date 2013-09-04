namespace NEventStore.Persistence.RavenDB
{
    public class RavenStreamHead
    {
        public string Id { get; set; }
        public string BucketId { get; set; }
        public string StreamId { get; set; }
        public int HeadRevision { get; set; }
        public int SnapshotRevision { get; set; }

        public int SnapshotAge
        {
            get { return HeadRevision - SnapshotRevision; } // set by map/reduce on the server
        }

        public static string GetStreamHeadId(string bucketId, string streamId)
        {
            string id = string.Format("StreamHeads/{0}/{1}", bucketId, streamId);

            return id;
        }
    }
}