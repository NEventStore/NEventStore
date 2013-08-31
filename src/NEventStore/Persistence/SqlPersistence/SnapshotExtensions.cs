namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    internal static class SnapshotExtensions
    {
        private const int BucketIdIndex = 0;
        private const int StreamIdIndex = 1;
        private const int StreamRevisionIndex = 2;
        private const int PayloadIndex = 3;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SnapshotExtensions));

        public static Snapshot GetSnapshot(this IDataRecord record, ISerialize serializer)
        {
            Logger.Verbose(Messages.DeserializingSnapshot);

            return new Snapshot(
                record[BucketIdIndex].ToString(),
                record[StreamIdIndex].ToString(),
                record[StreamRevisionIndex].ToInt(),
                serializer.Deserialize<object>(record, PayloadIndex));
        }
    }
}