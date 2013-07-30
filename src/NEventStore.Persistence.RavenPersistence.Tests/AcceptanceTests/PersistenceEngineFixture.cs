namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.RavenPersistence.Tests;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () => new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
        }
    }
}