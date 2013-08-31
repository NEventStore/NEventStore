namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.MongoPersistence.Tests;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () => new AcceptanceTestMongoPersistenceFactory().Build();
        }
    }
}