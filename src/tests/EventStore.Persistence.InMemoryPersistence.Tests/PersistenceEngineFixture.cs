using EventStore.Persistence.InMemoryPersistence;

namespace EventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () =>
                new InMemoryPersistenceEngine();
        }
    }
}