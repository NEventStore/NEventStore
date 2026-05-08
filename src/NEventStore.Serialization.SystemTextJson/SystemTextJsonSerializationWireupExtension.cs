using System.Text.Json;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json serialization wire-up extensions.
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
        /// <param name="knownTypes">Root types serialized without type metadata.</param>
        public static SerializationWireup UsingJsonSerialization(
            this PersistenceWireup wireup,
            JsonSerializerOptions? jsonSerializerOptions = null,
            params Type[]? knownTypes)
        {
            return wireup.UsingCustomSerialization(new SystemTextJsonSerializer(jsonSerializerOptions, knownTypes));
        }
    }
}
