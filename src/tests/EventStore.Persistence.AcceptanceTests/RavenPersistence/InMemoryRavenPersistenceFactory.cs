using EventStore.Persistence.RavenPersistence;
using Raven.Client;
using Raven.Client.Embedded;

namespace EventStore.Persistence.AcceptanceTests.RavenPersistence
{
    public class InMemoryRavenPersistenceFactory : RavenPersistenceFactory
    {
        public InMemoryRavenPersistenceFactory(RavenConfiguration config)
            : base(config)
        {
        }

        protected override IDocumentStore GetStore()
        {
            return new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
        }

    }
}