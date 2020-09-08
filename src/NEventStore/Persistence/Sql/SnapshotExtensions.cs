namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    internal static class SnapshotExtensions
    {
        private const int BucketIdIndex = 0;
        private const int StreamRevisionIndex = 2;
        private const int PayloadIndex = 3;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SnapshotExtensions));

        public static Snapshot GetSnapshot(this IDataRecord record, ISerialize serializer, string streamIdOriginal)
        {
            Logger.Verbose(Messages.DeserializingSnapshot);

            return new Snapshot(
                record[BucketIdIndex].ToString(),
                streamIdOriginal,
                record[StreamRevisionIndex].ToInt(),
                serializer.Deserialize<object>(record, PayloadIndex));
        }

        public static Snapshot GetSnapshot(this IDataRecord record, ISerializeSnapshots serializer, string streamIdOriginal)
        {
            Logger.Verbose(Messages.DeserializingSnapshot);

            return new Snapshot(
                record[BucketIdIndex].ToString(),
                streamIdOriginal,
                record[StreamRevisionIndex].ToInt(),
                serializer.Deserialize<object>(record, PayloadIndex));
        }

        public static T Deserialize<T>(this ISerializeSnapshots serializer, IDataRecord record, int index)
        {
            if (index >= record.FieldCount)
            {
                return default(T);
            }

            object value = record[index];
            if (value == null || value == DBNull.Value)
            {
                return default(T);
            }

            var bytes = (byte[])value;
            return bytes.Length == 0 ? default(T) : serializer.Deserialize<T>(bytes);
        }
    }
}