using System.Configuration;
using EventStore.Serialization;
using Microsoft.WindowsAzure.Storage;

namespace EventStore.Persistence.AzureTablesPersistence
{
    public class AzureTablesPersistenceFactory : IPersistenceFactory
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly ISerialize _serializer;

        public AzureTablesPersistenceFactory(string connectionStringName, ISerialize serializer)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            _serializer = serializer;
        }

        public IPersistStreams Build()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();

            return new AzureTablesPersistenceEngine(tableClient, _serializer);
        }
    }
}