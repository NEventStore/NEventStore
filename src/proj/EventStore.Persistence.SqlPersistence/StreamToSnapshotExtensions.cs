namespace EventStore.Persistence.SqlPersistence
{
	using System.Data;
	using Persistence;

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
				record[StreamNameIndex].ToString(),
				record[HeadRevisionIndex].ToLong(),
				record[SnapshotRevisionIndex].ToLong());
		}
	}
}