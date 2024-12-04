using System.Diagnostics;

namespace NEventStore.Serialization.MsgPack
{
    /// <summary>
    /// MsgPack serialization wireup extensions.
    /// </summary>
    public static class MsgPackSerializationWireupExtension
    {
        /// <summary>
        /// Use the MessagePack serializer.
        /// </summary>
        /// <param name="wireup">Wireup to extend</param>
        /// <returns>Serialization Wireup</returns>
        [DebuggerStepThrough]
        public static SerializationWireup UsingMsgPackSerialization(this PersistenceWireup wireup) => wireup.UsingCustomSerialization(new MsgPackSerializer());
    }
}
