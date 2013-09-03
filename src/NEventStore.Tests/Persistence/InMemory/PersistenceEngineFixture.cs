// ReSharper disable CheckNamespace
namespace NEventStore.Persistence.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Persistence.InMemory;
    using NEventStore.Persistence.InMemoryPersistence;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () =>
                new InMemoryPersistenceEngine();
        }
    }
}