namespace EventStore.Persistence.SqlPersistence
{
	using System.Data;
	using Persistence;

	internal static class StreamHeadExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamNameIndex = 1;
		private const int HeadRevisionIndex = 2;
		private const int SnapshotRevisionIndex = 3;

		public static StreamHead GetStreamToSnapshot(this IDataRecord record)
		{
			return new StreamHead(
				record[StreamIdIndex].ToGuid(),
				record[StreamNameIndex].ToString(),
				record[HeadRevisionIndex].ToLong(),
				record[SnapshotRevisionIndex].ToLong());
		}
	}
}