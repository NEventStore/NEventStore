using NEventStore.Logging;
using Microsoft.Extensions.Logging;
using NEventStore.Serialization;

namespace NEventStore
{
    /// <summary>
    ///   Represents the configuration for serialization.
    /// </summary>
    public static class SerializationWireupExtensions
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));

        /// <summary>
        /// Configure custom serialization.
        /// </summary>
        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupSetSerializer, serializer.GetType());
            }
            return new SerializationWireup(wireup, serializer);
        }
    }
}