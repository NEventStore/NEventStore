// ReSharper disable once CheckNamespace

#region

using NEventStore.Persistence.InMemory;

#endregion

namespace NEventStore.Persistence.AcceptanceTests;

public partial class PersistenceEngineFixture
{
    public PersistenceEngineFixture()
    {
        _createPersistence = _ =>
            new InMemoryPersistenceEngine();
    }
}