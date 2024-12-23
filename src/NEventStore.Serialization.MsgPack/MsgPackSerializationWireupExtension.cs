using MessagePack;
using System.Diagnostics;

namespace NEventStore.Serialization.MsgPack
{
    /// <summary>
    /// MsgPack serialization wire-up extensions.
    /// </summary>
    public static class MsgPackSerializationWireupExtension
    {
        /// <summary>
        /// Use the MessagePack serializer.
        /// </summary>
        /// <param name="wireup">Wire-up to extend</param>
        /// <param name="option">MsgPack serialization options</param>
        /// <returns>Serialization Wire-up</returns>
        [DebuggerStepThrough]
        public static SerializationWireup UsingMsgPackSerialization(this PersistenceWireup wireup, MessagePackSerializerOptions? option = null) => wireup.UsingCustomSerialization(new MsgPackSerializer(option));
    }
}
