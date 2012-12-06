using System;
using System.Data.Services.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence
{
    [DataServiceEntity]
    public class AzureTablesStreamHead : TableEntity
    {
        public int HeadRevision { get; set; }
        public int SnapshotRevision { get; set; }
        public int Unsnapshotted { get; set; }
    }

    public static class StreamHeadExtensions
    {
        public static AzureTablesStreamHead ToAzureTablesStreamHead(this StreamHead head)
        {
            var partitionKey = GetPartitionKey(head);
            var rowKey = GetRowKey(head);

            return new AzureTablesStreamHead
                       {
                           PartitionKey = partitionKey,
                           RowKey = rowKey,
                           HeadRevision = head.HeadRevision,
                           SnapshotRevision = head.SnapshotRevision
                       };
        }

        public static StreamHead ToStreamHead(this AzureTablesStreamHead azureHead)
        {
            var firstPartOfStreamId = BitConverter.GetBytes(long.Parse(azureHead.PartitionKey, CultureInfo.InvariantCulture));
            var secondPartOfStreamId = BitConverter.GetBytes(long.Parse(azureHead.RowKey, CultureInfo.InvariantCulture));

            var streamId = new Guid(firstPartOfStreamId.Concat(secondPartOfStreamId).ToArray());

            return new StreamHead(streamId, azureHead.HeadRevision, azureHead.SnapshotRevision);
        }

        public static TableOperation ToPointQuery(this StreamHead streamHead)
        {
            var partitionKey = GetPartitionKey(streamHead);
            var rowKey = GetRowKey(streamHead);

            return TableOperation.Retrieve<AzureTablesStreamHead>(partitionKey, rowKey);
        }

        internal static string GetPartitionKey(StreamHead streamHead)
        {
            var streamIdBytes = streamHead.StreamId.ToByteArray();
            var partitionKey = BitConverter.ToInt64(streamIdBytes, 0).ToString(CultureInfo.InvariantCulture);

            return partitionKey;
        }

        internal static string GetRowKey(StreamHead streamHead)
        {
            var streamIdBytes = streamHead.StreamId.ToByteArray();
            var rowKey = BitConverter.ToInt64(streamIdBytes, 8).ToString(CultureInfo.InvariantCulture);

            return rowKey;
        }
    }
}