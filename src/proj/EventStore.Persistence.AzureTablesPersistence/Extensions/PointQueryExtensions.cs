using System;
using System.Globalization;
using EventStore.Persistence.AzureTablesPersistence.Datastructures;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence.Extensions
{
    public static class PointQueryExtensions
    {
        public static TableOperation ToStreamHeadPointQuery(this Guid streamId)
        {
            var streamIdBytes = streamId.ToByteArray();
            var partitionKey = BitConverter.ToInt64(streamIdBytes, 0).ToString(CultureInfo.InvariantCulture);
            var rowKey = BitConverter.ToInt64(streamIdBytes, 8).ToString(CultureInfo.InvariantCulture);

            return TableOperation.Retrieve<AzureStreamHead>(partitionKey, rowKey);
        }

        public static TableOperation ToPointQuery(this Commit commit)
        {
            return TableOperation.Retrieve<AzureCommit>(commit.GetPartitionKey(), commit.GetRowKey());
        }
    }
}