namespace NEventStore.Persistence.MongoDB
{
    using System.Configuration;
    using global::MongoDB.Driver;
    using NEventStore.Serialization;

    public class MongoPersistenceFactory : IPersistenceFactory
    {
        private readonly string _connectionName;
        private readonly IDocumentSerializer _serializer;

        public MongoPersistenceFactory(string connectionName, IDocumentSerializer serializer)
        {
            _connectionName = connectionName;
            _serializer = serializer;
        }

        public virtual IPersistStreams Build()
        {
            string connectionString = TransformConnectionString(GetConnectionString());
            var builder = new MongoUrlBuilder(connectionString);
            MongoDatabase database = (new MongoClient(connectionString)).GetServer().GetDatabase(builder.DatabaseName);
            return new MongoPersistenceEngine(database, _serializer);
        }

        protected virtual string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[_connectionName].ConnectionString;
        }

        protected virtual string TransformConnectionString(string connectionString)
        {
            return connectionString;
        }
    }
}