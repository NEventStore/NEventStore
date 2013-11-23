// ReSharper disable once CheckNamespace
namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.MongoDB.Tests;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = _ => new AcceptanceTestMongoPersistenceFactory().Build();
        }
    }
}