namespace EventStore.Persistence.SqlPersistence
{
    using System.Data;
    using Logging;
    using Serialization;

    internal static class SnapshotExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamRevisionIndex = 1;
		private const int PayloadIndex = 2;
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SnapshotExtensions));

		public static Snapshot GetSnapshot(this IDataRecord record, ISerialize serializer)
		{
			Logger.Verbose(Messages.DeserializingSnapshot);

			return new Snapshot(
				record[StreamIdIndex].ToGuid(),
				record[StreamRevisionIndex].ToInt(),
				serializer.Deserialize<object>(record, PayloadIndex));
		}
	}
}