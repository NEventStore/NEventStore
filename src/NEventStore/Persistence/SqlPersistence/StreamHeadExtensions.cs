namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;

    internal static class StreamHeadExtensions
    {
        private const int StreamIdIndex = 1;
        private const int HeadRevisionIndex = 2;
        private const int SnapshotRevisionIndex = 3;

        public static StreamHead GetStreamToSnapshot(this IDataRecord record)
        {
            return new StreamHead(
                record[StreamIdIndex].ToString(),
                record[HeadRevisionIndex].ToInt(),
                record[SnapshotRevisionIndex].ToInt());
        }
    }
}