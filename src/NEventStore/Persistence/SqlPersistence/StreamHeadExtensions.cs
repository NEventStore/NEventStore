namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;

    internal static class StreamHeadExtensions
	{
		private const int StreamIdIndex = 0;
		private const int HeadRevisionIndex = 1;
		private const int SnapshotRevisionIndex = 2;

		public static StreamHead GetStreamToSnapshot(this IDataRecord record)
		{
			return new StreamHead(
				record[StreamIdIndex].ToGuid(),
				record[HeadRevisionIndex].ToInt(),
				record[SnapshotRevisionIndex].ToInt());
		}
	}
}