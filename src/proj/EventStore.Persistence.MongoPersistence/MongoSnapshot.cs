namespace EventStore.Persistence.MongoPersistence
{
	using MongoDB.Bson;

	internal class MongoSnapshot
	{
		public MongoSnapshotId Id { get; set; }
		public BsonValue Payload { get; set; }
	}
}