namespace NEventStore.Persistence.MongoPersistence
{
    using System;
    using MongoDB.Driver;
    using NEventStore.Serialization;

    public class MongoPersistenceFactory : IPersistenceFactory
    {
        private readonly Func<string> _connectionStringProvider;
        private readonly IDocumentSerializer _serializer;

        public MongoPersistenceFactory(Func<string> connectionStringProvider, IDocumentSerializer serializer)
        {
            _connectionStringProvider = connectionStringProvider;
            _serializer = serializer;
        }

        public virtual IPersistStreams Build()
        {
            string connectionString = _connectionStringProvider();
            var builder = new MongoUrlBuilder(connectionString);
            MongoDatabase database = (new MongoClient(connectionString)).GetServer().GetDatabase(builder.DatabaseName);
            return new MongoPersistenceEngine(database, _serializer);
        }
    }
}