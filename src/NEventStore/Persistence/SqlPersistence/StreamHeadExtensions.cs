namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;

    internal static class StreamHeadExtensions
    {
        private const int BucketIdIndex = 0;
        private const int StreamIdIndex = 2;
        private const int HeadRevisionIndex = 3;
        private const int SnapshotRevisionIndex = 4;

        public static StreamHead GetStreamToSnapshot(this IDataRecord record)
        {
            return new StreamHead(
                record[BucketIdIndex].ToString(),
                record[StreamIdIndex].ToString(),
                record[HeadRevisionIndex].ToInt(),
                record[SnapshotRevisionIndex].ToInt());
        }
    }
}