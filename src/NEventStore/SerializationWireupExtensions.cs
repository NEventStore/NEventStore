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
        /// Configure binary serialization.
        /// </summary>
        [Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.")]
        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupSetSerializer, "Binary");
            }
            return new SerializationWireup(wireup, new BinarySerializer());
        }

        /// <summary>
        /// Configure custom serialization.
        /// </summary>
        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupSetSerializer, "Custom" + serializer.GetType());
            }
            return new SerializationWireup(wireup, serializer);
        }
    }
}