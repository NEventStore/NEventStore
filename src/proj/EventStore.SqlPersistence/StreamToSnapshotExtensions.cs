namespace EventStore.SqlPersistence
{
	using System.Data;

	internal static class StreamToSnapshotExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamNameIndex = 1;
		private const int HeadRevisionIndex = 2;
		private const int SnapshotRevisionIndex = 3;

		public static StreamToSnapshot GetStreamToSnapshot(this IDataRecord record)
		{
			return new StreamToSnapshot(
				record[StreamIdIndex].ToGuid(),
				(string)record[StreamNameIndex],
				(long)record[HeadRevisionIndex],
				(long)record[SnapshotRevisionIndex]);
		}
	}
}