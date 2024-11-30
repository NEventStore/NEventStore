namespace NEventStore;

using Logging;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.InMemory;

public static class PersistenceWireupExtensions
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticPipelineHook));

    public static PersistenceWireup UsingInMemoryPersistence(this Wireup wireup)
    {
        Logger.LogInformation(Resources.WireupSetPersistenceEngine, "InMemoryPersistenceEngine");
        wireup.With<IPersistStreams>(new InMemoryPersistenceEngine());

        return new PersistenceWireup(wireup);
    }

    public static int Records(this int records)
    {
        return records;
    }
}