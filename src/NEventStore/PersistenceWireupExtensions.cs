namespace NEventStore
{
    using Logging;
    using NEventStore.Persistence;
    using NEventStore.Persistence.InMemory;

    public static class PersistenceWireupExtensions
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(OptimisticPipelineHook));

        public static PersistenceWireup UsingInMemoryPersistence(this Wireup wireup)
        {
            Logger.Info(Resources.WireupSetPersistenceEngine, "InMemoryPersistenceEngine");
            wireup.With<IPersistStreams>(new InMemoryPersistenceEngine());

            return new PersistenceWireup(wireup);
        }

        public static int Records(this int records)
        {
            return records;
        }
    }
}