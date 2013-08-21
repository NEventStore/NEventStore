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

        public static string GetStreamHeadId(string bucketId, string streamId, string partition)
        {
            string id = string.Format("StreamHeads/{0}/{1}", bucketId, streamId);

            if (!string.IsNullOrEmpty(partition))
            {
                id = string.Format("{0}/{1}", partition, id);
            }

            return id;
        }
    }
}