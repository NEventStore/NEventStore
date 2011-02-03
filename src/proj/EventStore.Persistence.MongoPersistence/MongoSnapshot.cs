namespace EventStore.Persistence.MongoPersistence
{
	internal class MongoSnapshot
	{
		public MongoSnapshotId Id { get; set; }
		public byte[] Payload { get; set; }
	}
}