namespace EventStore.Persistence.MongoDBPersistence
{
	using System;

	public class MongoDBSnapshotId
	{
		public Guid StreamId { get; private set; }
		public int StreamRevision { get; private set; }

		public MongoDBSnapshotId(Guid streamId, int streamRevision)
		{
			StreamId = streamId;
			StreamRevision = streamRevision;
		}
	}
}