namespace EventStore.Persistence.SqlPersistence
{
	using System.Data;
	using Serialization;

	internal static class SnapshotExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamRevisionIndex = 1;
		private const int PayloadIndex = 2;

		public static Snapshot GetSnapshot(this IDataRecord record, ISerialize serializer)
		{
			return new Snapshot(
				record[StreamIdIndex].ToGuid(),
				record[StreamRevisionIndex].ToInt(),
				serializer.Deserialize(record, PayloadIndex));
		}
	}
}