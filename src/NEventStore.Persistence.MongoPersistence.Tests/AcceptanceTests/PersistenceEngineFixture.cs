namespace NEventStore.Persistence.AcceptanceTests
{
    using EventStore.Persistence.MongoPersistence.Tests;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () => new AcceptanceTestMongoPersistenceFactory().Build();
        }
    }
}