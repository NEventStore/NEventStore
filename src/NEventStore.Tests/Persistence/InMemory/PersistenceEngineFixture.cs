// ReSharper disable once CheckNamespace
using NEventStore.Persistence.InMemory;

namespace NEventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = _ =>
                new InMemoryPersistenceEngine();
        }
    }
}
