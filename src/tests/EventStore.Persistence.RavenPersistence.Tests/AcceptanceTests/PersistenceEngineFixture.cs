using EventStore.Persistence.RavenPersistence.Tests;

namespace NEventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () => 
                new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
        }
    }
}