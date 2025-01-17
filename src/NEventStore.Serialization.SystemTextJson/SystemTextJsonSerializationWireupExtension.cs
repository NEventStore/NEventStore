using System.Text.Json;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// Newtonsoft Json serialization wire-up extensions.
    /// </summary>
    public static class SystemTextJsonSerializationWireupExtension
    {
        /// <summary>
        /// Specify we want to use Json serialization using System.Text.Json
        /// </summary>
        /// <param name="wireup">The persistence wire-up.</param>
        /// <param name="jsonSerializerOptions">
        /// Allows to customize some Serializer options, however some of them will
        /// be under control of this specific implementation and will be overwritten no matter
        /// what the user specifies:
        /// - DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        /// </param>
        public static SerializationWireup UsingJsonSerialization(
            this PersistenceWireup wireup,
            JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return wireup.UsingCustomSerialization(new SystemTextJsonSerializer(jsonSerializerOptions));
        }
    }
}
