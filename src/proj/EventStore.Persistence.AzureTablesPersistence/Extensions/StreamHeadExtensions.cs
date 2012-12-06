using System;
using System.Globalization;
using System.Linq;
using EventStore.Persistence.AzureTablesPersistence.Datastructures;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence.Extensions
{
    public static class StreamHeadExtensions
    {
        public static AzureStreamHead ToAzureTablesStreamHead(this StreamHead head)
        {
            var partitionKey = GetPartitionKey(head);
            var rowKey = GetRowKey(head);

            return new AzureStreamHead
                       {
                           PartitionKey = partitionKey,
                           RowKey = rowKey,
                           HeadRevision = head.HeadRevision,
                           SnapshotRevision = head.SnapshotRevision
                       };
        }

        public static TableOperation ToPointQuery(this StreamHead streamHead)
        {
            var partitionKey = GetPartitionKey(streamHead);
            var rowKey = GetRowKey(streamHead);

            return TableOperation.Retrieve<AzureStreamHead>(partitionKey, rowKey);
        }

        internal static string GetPartitionKey(this StreamHead streamHead)
        {
            var streamIdBytes = streamHead.StreamId.ToByteArray();
            var partitionKey = BitConverter.ToInt64(streamIdBytes, 0).ToString(CultureInfo.InvariantCulture);

            return partitionKey;
        }

        internal static string GetRowKey(this StreamHead streamHead)
        {
            var streamIdBytes = streamHead.StreamId.ToByteArray();
            var rowKey = BitConverter.ToInt64(streamIdBytes, 8).ToString(CultureInfo.InvariantCulture);

            return rowKey;
        }
    }

    public static class AzureStreamHeadExtensions
    {
        public static StreamHead ToStreamHead(this AzureStreamHead azureHead)
        {
            var firstPartOfStreamId = BitConverter.GetBytes(long.Parse(azureHead.PartitionKey, CultureInfo.InvariantCulture));
            var secondPartOfStreamId = BitConverter.GetBytes(long.Parse(azureHead.RowKey, CultureInfo.InvariantCulture));

            var streamId = new Guid(firstPartOfStreamId.Concat(secondPartOfStreamId).ToArray());

            return new StreamHead(streamId, azureHead.HeadRevision, azureHead.SnapshotRevision);
        }
    }
}