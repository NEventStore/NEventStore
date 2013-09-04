namespace NEventStore.Persistence.RavenDB.Tests
{
    using NEventStore.Persistence.RavenDB;
    using Raven.Client;
    using Raven.Client.Embedded;

    public class InMemoryRavenPersistenceFactory : RavenPersistenceFactory
    {
        public InMemoryRavenPersistenceFactory(RavenConfiguration config) : base(config)
        {}

        protected override IDocumentStore GetStore()
        {
            return new EmbeddableDocumentStore {RunInMemory = true}.Initialize();
        }
    }
}