using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore
{
    /// <summary>
    /// Provides extension methods to configure logging.
    /// </summary>
    public static class LoggingWireupExtensions
    {
        /// <summary>
        /// Configures NEventStore to use the specified logger factory.
        /// </summary>
        public static Wireup WithLoggerFactory(this Wireup wireup, ILoggerFactory loggerFactory)
        {
            return wireup.LogTo(type => loggerFactory.CreateLogger(type));
        }

        /// <summary>
        /// Configures NEventStore to use the specified logger.
        /// </summary>
        public static Wireup LogTo(this Wireup wireup, Func<Type, ILogger> logger)
        {
            LogFactory.BuildLogger = logger;
            return wireup;
        }
    }
}