namespace NEventStore
{
    using Logging;
    using NEventStore.Serialization;

    public static class SerializationWireupExtensions
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));

#if !NETSTANDARD1_6
        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
            Logger.Info(Resources.WireupSetSerializer, "Binary");
            return new SerializationWireup(wireup, new BinarySerializer());
        }
#endif

        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            Logger.Info(Resources.WireupSetSerializer, "Custom" + serializer.GetType());
            return new SerializationWireup(wireup, serializer);
        }

   
    }
}