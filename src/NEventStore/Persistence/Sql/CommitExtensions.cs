namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public static class CommitExtensions
    {
        private const int BucketIdIndex = 0;
        private const int StreamIdIndex = 1;
        private const int StreamIdOriginalIndex = 2;
        private const int StreamRevisionIndex = 3;
        private const int CommitIdIndex = 4;
        private const int CommitSequenceIndex = 5;
        private const int CommitStampIndex = 6;
        private const int CheckpointIndex = 7;
        private const int HeadersIndex = 8;
        private const int PayloadIndex = 9;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (CommitExtensions));

        public static ICommit GetCommit(this IDataRecord record, ISerialize serializer)
        {
            Logger.Verbose(Messages.DeserializingCommit, serializer.GetType());
            var headers = serializer.Deserialize<Dictionary<string, object>>(record, HeadersIndex);
            var events = serializer.Deserialize<List<EventMessage>>(record, PayloadIndex);

            return new Commit(record[BucketIdIndex].ToString(),
                record[StreamIdOriginalIndex].ToString(),
                record[StreamRevisionIndex].ToInt(),
                record[CommitIdIndex].ToGuid(),
                record[CommitSequenceIndex].ToInt(),
                record[CommitStampIndex].ToDateTime(),
                new LongCheckpoint(record[CheckpointIndex].ToLong()).Value,
                headers,
                events);
        }

        public static string StreamId(this IDataRecord record)
        {
            return record[StreamIdIndex].ToString();
        }

        public static int CommitSequence(this IDataRecord record)
        {
            return record[CommitSequenceIndex].ToInt();
        }

        public static int CheckpointNumber(this IDataRecord record)
        {
            return record[CheckpointIndex].ToInt();
        }

        public static T Deserialize<T>(this ISerialize serializer, IDataRecord record, int index)
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

            var bytes = (byte[]) value;
            return bytes.Length == 0 ? default(T) : serializer.Deserialize<T>(bytes);
        }
    }
}