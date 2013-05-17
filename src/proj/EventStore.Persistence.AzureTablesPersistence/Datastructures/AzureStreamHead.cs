using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence.Datastructures
{
    public class AzureStreamHead : TableEntity
    {
        public int HeadRevision { get; set; }
        public int SnapshotRevision { get; set; }
        public int Unsnapshotted { get; set; }
    }
}