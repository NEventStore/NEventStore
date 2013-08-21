namespace NEventStore.Persistence.MongoPersistence
{
    public static class MongoFields
    {
        public const string BucketId = "BucketId";
        public const string CommitId = "CommitId";
        public const string CommitStamp = "CommitStamp";
        public const string CommitSequence = "CommitSequence";
        public const string Dispatched = "Dispatched";
        public const string Events = "Events";
        public const string Headers = "Headers";
        public const string HeadRevision = "HeadRevision";
        public const string Id = "_id";
        public const string Payload = "Payload";
        public const string SnapshotRevision = "SnapshotRevision";
        public const string StreamId = "StreamId";
        public const string StreamRevision = "StreamRevision";
        public const string Unsnapshotted = "Unsnapshotted";
    }
}