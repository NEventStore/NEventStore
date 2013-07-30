namespace NEventStore.Persistence.RavenPersistence.Tests
{
    using Raven.Client;
    using Raven.Client.Embedded;
    using NEventStore.Persistence.RavenPersistence;

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