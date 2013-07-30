namespace NEventStore.Persistence.AcceptanceTests
{
    using RavenPersistence.Tests;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () => 
                new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
        }
    }
}