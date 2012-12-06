using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence.Datastructures
{
    public class AzureSnapshot : TableEntity
    {
        public byte[] Payload;
    }
}