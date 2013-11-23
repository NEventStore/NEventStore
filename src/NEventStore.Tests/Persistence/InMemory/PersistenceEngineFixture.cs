// ReSharper disable once CheckNamespace
namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.InMemory;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = _ =>
                new InMemoryPersistenceEngine();
        }
    }
}