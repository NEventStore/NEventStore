namespace NEventStore
{
    using Logging;
    using Microsoft.Extensions.Logging;
    using NEventStore.Serialization;

    public static class SerializationWireupExtensions
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));

        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
            Logger.LogInformation(Resources.WireupSetSerializer, "Binary");
            return new SerializationWireup(wireup, new BinarySerializer());
        }

        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            Logger.LogInformation(Resources.WireupSetSerializer, "Custom" + serializer.GetType());
            return new SerializationWireup(wireup, serializer);
        }
    }
}