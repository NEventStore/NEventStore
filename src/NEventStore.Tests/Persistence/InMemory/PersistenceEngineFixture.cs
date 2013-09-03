// ReSharper disable CheckNamespace
namespace NEventStore.Persistence.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Persistence.InMemory;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () =>
                new InMemoryPersistenceEngine();
        }
    }
}