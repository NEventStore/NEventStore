using NEventStore.Logging;
using Microsoft.Extensions.Logging;
using NEventStore.Persistence;
using NEventStore.Persistence.InMemory;

namespace NEventStore
{
    /// <summary>
    /// Persistence wireup extensions.
    /// </summary>
    public static class PersistenceWireupExtensions
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticPipelineHook));

        /// <summary>
        /// Configures the persistence engine to use the in-memory persistence engine.
        /// </summary>
        public static PersistenceWireup UsingInMemoryPersistence(this Wireup wireup)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupSetPersistenceEngine, "InMemoryPersistenceEngine");
            }
            wireup.Register<IPersistStreams>(new InMemoryPersistenceEngine());

            return new PersistenceWireup(wireup);
        }
    }
}