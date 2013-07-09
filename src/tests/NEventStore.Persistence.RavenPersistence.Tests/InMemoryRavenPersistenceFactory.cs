using Raven.Client;
using Raven.Client.Embedded;

namespace EventStore.Persistence.RavenPersistence.Tests
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