namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using MongoDB.Bson.DefaultSerializer;

	public class MongoStreamHead
	{
		[BsonId]
		public Guid StreamId { get; private set; }
		public int HeadRevision { get; private set; }
		public int SnapshotRevision { get; private set; }

		public MongoStreamHead(Guid streamId, int headRevision, int snapshotRevision)
		{
			this.StreamId = streamId;
			this.HeadRevision = headRevision;
			this.SnapshotRevision = snapshotRevision;
		}
	}
}