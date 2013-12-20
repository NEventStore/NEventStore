namespace NEventStore.Persistence.MongoDB
{
    using System;
    using global::MongoDB.Driver;
    using NEventStore.Serialization;

    public class MongoPersistenceFactory : IPersistenceFactory
    {
        private readonly Func<string> _connectionStringProvider;
        private readonly IDocumentSerializer _serializer;
		private readonly MongoPersistenceOptions _options;

	    public MongoPersistenceFactory(Func<string> connectionStringProvider, IDocumentSerializer serializer, MongoPersistenceOptions options = null)
        {
            _connectionStringProvider = connectionStringProvider;
            _serializer = serializer;
	        _options = options ?? new MongoPersistenceOptions();
        }

        public virtual IPersistStreams Build()
        {
            string connectionString = _connectionStringProvider();
	        MongoDatabase database = _options.ConnectToDatabase(connectionString);
            return new MongoPersistenceEngine(database, _serializer, _options);
        }
    }
}
