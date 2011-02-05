using EventStore.Serialization;
using Raven.Client;
using Raven.Client.Document;

namespace EventStore.Persistence.RavenPersistence
{
    public class RavenPersistenceFactory : IPersistenceFactory
    {
        private readonly ISerialize serializer;

        protected string ConnectionStringName { get; private set; }

        public RavenPersistenceFactory(string connectionName, ISerialize serializer)
        {
            ConnectionStringName = connectionName;
            this.serializer = serializer;
        }

        public virtual IPersistStreams Build()
        {
            var store = GetStore();
            return new RavenPersistenceEngine(store, serializer);
        }

        protected virtual IDocumentStore GetStore()
        {
            var store = new DocumentStore { ConnectionStringName = ConnectionStringName };
            store.Initialize();

            return store;
        }
    }
}