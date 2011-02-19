namespace EventStore.Persistence.MongoPersistence
{
	using System;

	public class MongoSnapshotId
	{
		public Guid StreamId { get; private set; }
		public int StreamRevision { get; private set; }

		public MongoSnapshotId(Guid streamId, int streamRevision)
		{
			this.StreamId = streamId;
			this.StreamRevision = streamRevision;
		}
	}
}