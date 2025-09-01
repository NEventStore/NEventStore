using System.Text.Json;
using System.Text.Json.Serialization;
using NEventStore.Serialization.SystemTextJson.Converters;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json serialization wire-up extensions
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
        /// <br/>
        /// <br/>
        /// - <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> = <see cref="JsonIgnoreCondition.WhenWritingNull" />
        /// <br/>
        /// - Always include <see cref="SystemTextJsonTypeInfoResolver"/> in <see cref="JsonSerializerOptions.TypeInfoResolverChain"/>
        /// <br/>
        /// - Always include <see cref="SnapshotJsonConverter"/> and <see cref="EventMessageJsonConverter"/> in
        /// <see cref="JsonSerializerOptions.Converters"/> when serializing
        /// - Always include <see cref="DictionaryOfStringAndObjectJsonConverter"/> in
        /// <see cref="JsonSerializerOptions.Converters"/> when deserializing
        /// </param>
        public static SerializationWireup UsingJsonSerialization(
            this PersistenceWireup wireup,
            JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return wireup.UsingCustomSerialization(new SystemTextJsonSerializer(jsonSerializerOptions));
        }
    }
}
