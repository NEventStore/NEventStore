using Newtonsoft.Json;

namespace NEventStore.Serialization.Json
{
    /// <summary>
    /// Newtonsoft Json serialization wire-up extensions.
    /// </summary>
    public static class JsonSerializationWireupExtension
    {
        /// <summary>
        /// Specify we want to use Json serialization using Newtonsoft.Json
        /// </summary>
        /// <param name="jsonSerializerSettings">
        /// Allows to customize some Serializer options, however some of them will
        /// be under control of this specific implementation and will be overwritten no matter
        /// what the user specifies:
        /// - TypeNameHandling = TypeNameHandling.All
        /// - DefaultValueHandling = DefaultValueHandling.Ignore
        /// - NullValueHandling = NullValueHandling.Ignore
        /// </param>
        public static SerializationWireup UsingJsonSerialization(
            this PersistenceWireup wireup,
            JsonSerializerSettings jsonSerializerSettings = null)
        {
            return wireup.UsingCustomSerialization(new JsonSerializer(jsonSerializerSettings));
        }
    }
}
