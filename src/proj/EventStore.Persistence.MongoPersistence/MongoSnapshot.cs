namespace EventStore.Persistence.MongoPersistence
{
	using MongoDB.Bson;

	public class MongoSnapshot
	{
		public MongoSnapshotId Id { get; set; }
		public BsonValue Payload { get; set; }
	}
}