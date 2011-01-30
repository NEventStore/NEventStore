namespace EventStore.Persistence.MongoDBPersistence
{
	public class MongoDBSnapshot
	{
		public MongoDBSnapshotId Id { get; set; }
		public byte[] Payload { get; set; }
	}
}