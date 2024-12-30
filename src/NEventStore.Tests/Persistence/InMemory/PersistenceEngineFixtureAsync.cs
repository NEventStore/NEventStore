// ReSharper disable once CheckNamespace
using NEventStore.Persistence.InMemory;

namespace NEventStore.Persistence.AcceptanceTests.Async
{
    public partial class PersistenceEngineFixtureAsync
    {
        public PersistenceEngineFixtureAsync()
        {
            _createPersistence = _ =>
                new InMemoryPersistenceEngine();
        }
    }
}