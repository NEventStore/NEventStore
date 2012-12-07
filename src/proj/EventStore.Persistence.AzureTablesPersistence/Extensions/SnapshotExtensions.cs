using System;
using System.Globalization;
using EventStore.Persistence.AzureTablesPersistence.Datastructures;
using EventStore.Serialization;

namespace EventStore.Persistence.AzureTablesPersistence.Extensions
{
    public static class SnapshotExtensions
    {
        public static AzureSnapshot ToAzureSnapshot(this Snapshot snapshot, ISerialize serializer)
        {
            var partitionKey = snapshot.GetPartitionKey();
            var rowKey = snapshot.GetRowKey();
            var payload = serializer.Serialize(snapshot.Payload);

            return new AzureSnapshot
                       {
                           PartitionKey = partitionKey,
                           RowKey = rowKey,
                           Payload = payload,
                       };
        }

        private static string GetPartitionKey(this Snapshot snapshot)
        {
            return snapshot.StreamId.ToString();
        }

        private static string GetRowKey(this Snapshot snapshot)
        {
            return IntegralRowKeyHelpers.EncodeDouble(snapshot.StreamRevision);
        }
    }

    public static class AzureSnapshotExtensions
    {
        public static Snapshot ToSnapshot(this AzureSnapshot azureSnapshot, ISerialize serializer)
        {
            var streamId = azureSnapshot.GetStreamId();
            var streamRevision = azureSnapshot.GetStreamRevision();
            var payload = serializer.Deserialize<object>(azureSnapshot.Payload);

            return new Snapshot(streamId, streamRevision, payload);
        }

        private static Guid GetStreamId(this AzureSnapshot azureSnapshot)
        {
            return Guid.Parse(azureSnapshot.PartitionKey);
        }

        public static int GetStreamRevision(this AzureSnapshot azureSnapshot)
        {
            return (int)IntegralRowKeyHelpers.DecodeDouble(azureSnapshot.RowKey);
        }
    }
}