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
        /// - PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
        /// - SnapshotJsonConverter and ObjectJsonConverter will be added to the Converters collection
        /// </param>
        /// <param name="knownTypes">
        /// Every Type specified here will be serialized without root type metadata.
        /// Every other root type and polymorphic object value will be serialized with Newtonsoft-compatible
        /// $type metadata.
        /// </param>
        public static SerializationWireup UsingJsonSerialization(
            this PersistenceWireup wireup,
            JsonSerializerOptions? jsonSerializerOptions = null,
            params Type[]? knownTypes)
        {
            return wireup.UsingCustomSerialization(new SystemTextJsonSerializer(jsonSerializerOptions, knownTypes));
        }
    }
}
