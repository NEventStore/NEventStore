namespace EventStore.Persistence.MongoDBPersistence
{
    using System;
    using MongoDB.Bson.DefaultSerializer;

    public class MongoDBStreamHead
    {
        [BsonId]
        public Guid StreamId { get; private set; }
        public int HeadRevision { get; private set; }
        public int SnapshotRevision { get; private set; }

        public MongoDBStreamHead(Guid streamId, int headRevision, int snapshotRevision)
        {
            StreamId = streamId;
            HeadRevision = headRevision;
            SnapshotRevision = snapshotRevision;
        }
    }
}