namespace EventStore
{
    public static class AzureTablesPersistenceWireupExtensions
    {
        public static PersistenceWireup UsingAzureTablesPersistence(
            this Wireup wireup, string connectionName)
        {
            return new AzureTablesPersistenceWireup(wireup, connectionName);
        }
    }
}