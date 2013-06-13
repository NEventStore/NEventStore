using EventStore.Persistence.MongoPersistence.Tests;

namespace EventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () =>
                new AcceptanceTestMongoPersistenceFactory().Build();
        }
    }
}