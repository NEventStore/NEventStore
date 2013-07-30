namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.InMemoryPersistence;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            createPersistence = () =>
                new InMemoryPersistenceEngine();
        }
    }
}