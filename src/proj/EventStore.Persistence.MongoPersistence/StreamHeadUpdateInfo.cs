using System;

namespace EventStore.Persistence.MongoPersistence
{
	internal class StreamHeadUpdateInfo
	{
		public Guid StreamId { get; private set; }
		public int EventCount { get; private set; }

		public StreamHeadUpdateInfo(Guid streamId, int eventCount)
		{
			StreamId = streamId;
			EventCount = eventCount;
		}
	}
}