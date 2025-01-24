using System.Diagnostics;

namespace NEventStore.Serialization.Binary
{
    /// <summary>
    /// Binary serialization wire-up extensions.
    /// </summary>
    public static class BinarySerializationWireupExtension
    {
        /// <summary>
        /// Use the MessagePack serializer.
        /// </summary>
        /// <param name="wireup">Wire-up to extend</param>
        /// <returns>Serialization Wire-up</returns>
        [DebuggerStepThrough]
        [Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.")]
        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
#if NET8_0_OR_GREATER
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
#endif
            return wireup.UsingCustomSerialization(new BinarySerializer());
        }
    }
}
