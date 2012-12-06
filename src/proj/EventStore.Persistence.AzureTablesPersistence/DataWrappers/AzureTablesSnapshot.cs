using System;
using System.Data.Services.Common;
using System.Globalization;
using System.Linq.Expressions;
using EventStore.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence
{
    [DataServiceEntity]
    public class AzureTablesSnapshot : TableEntity
    {
        public byte[] Payload;
    }

    public static class SnapshotExtensions
    {
        public static AzureTablesSnapshot ToAzureTablesSnapshot(this Snapshot snapshot, ISerialize serializer)
        {
            var partitionKey = GetPartitionKey(snapshot);
            var rowKey = GetRowKey(snapshot);
            var payload = serializer.Serialize(snapshot.Payload);

            return new AzureTablesSnapshot
                       {
                           PartitionKey = partitionKey,
                           RowKey = rowKey,
                           Payload = payload,
                       };
        }

        public static Snapshot ToSnapshot(this AzureTablesSnapshot azureSnapshot, ISerialize serializer)
        {
            var streamId = Guid.Parse(azureSnapshot.PartitionKey);
            var streamRevision = int.Parse(azureSnapshot.RowKey, CultureInfo.InvariantCulture);
            var payload = serializer.Deserialize<object>(azureSnapshot.Payload);

            return new Snapshot(streamId, streamRevision, payload);
        }

        public static Expression<Func<AzureTablesSnapshot, bool>> ToPointQuery(this Snapshot snapshot)
        {
            var partitionKey = GetPartitionKey(snapshot);
            var rowKey = GetRowKey(snapshot);

            return (x => x.PartitionKey == partitionKey && x.RowKey == rowKey);
        }

        private static string GetPartitionKey(Snapshot snapshot)
        {
            return snapshot.StreamId.ToString();
        }

        private static string GetRowKey(Snapshot snapshot)
        {
            return snapshot.StreamRevision.ToString(CultureInfo.InvariantCulture);
        }
    }
}