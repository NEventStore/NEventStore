using EventStore.Serialization;

namespace EventStore.Persistence.AzureTablesPersistence.Wireup
{
    public static class AzureTablesPersistenceWireupExtensions
    {
        public static PersistenceWireup UsingAzureTablesPersistence(
            this EventStore.Wireup wireup, string connectionName)
        {
            return new AzureTablesPersistenceWireup(wireup, connectionName);
        }
    }
}