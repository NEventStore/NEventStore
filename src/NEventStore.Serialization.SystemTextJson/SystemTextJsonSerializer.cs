using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Serialization.Json;
using NEventStore.Serialization.SystemTextJson.Converters;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// JSON serializer using the System.Text.Json library
    /// </summary>
    public class SystemTextJsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(SystemTextJsonSerializer));

        private readonly JsonSerializerOptions _writeOptions;
        private readonly JsonSerializerOptions _readOptions;

        /// <summary>
        /// NEventStore Json Serialization using System.Text.Json
        /// </summary>
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
        public SystemTextJsonSerializer(JsonSerializerOptions? jsonSerializerOptions)
        {
            _writeOptions = new JsonSerializerOptions(jsonSerializerOptions ?? new JsonSerializerOptions());
            _writeOptions.TypeInfoResolverChain.Insert(0, new SystemTextJsonTypeInfoResolver());
            _writeOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            _writeOptions.Converters.Add(new SnapshotJsonConverter());
            _writeOptions.Converters.Add(new EventMessageJsonConverter());

            // We don't need to include the Dictionary<string, object> converter when writing, only when reading
            _readOptions = new JsonSerializerOptions(_writeOptions);
            _readOptions.Converters.Add(new DictionaryOfStringAndObjectJsonConverter());
        }

        /// <inheritdoc/>
        public virtual void Serialize<T>(Stream output, T graph) where T : notnull
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }
            using var streamWriter = new StreamWriter(output, Encoding.UTF8);
            using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream);
            Serialize(jsonWriter, graph);
        }

        /// <inheritdoc/>
        public virtual T? Deserialize<T>(Stream input)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }
            using var streamReader = new StreamReader(input, Encoding.UTF8);
            var jsonBytes = Encoding.UTF8.GetBytes(streamReader.ReadToEnd());
            var jsonReader = new Utf8JsonReader(jsonBytes);
            return Deserialize<T>(jsonReader);
        }

        /// <summary>
        /// Serialize an object to a Utf8JsonWriter.
        /// </summary>
        protected virtual void Serialize(Utf8JsonWriter writer, object graph)
        {
            JsonSerializer.Serialize(writer, graph, _writeOptions);
        }

        /// <summary>
        /// Deserialize an object from a Utf8JsonReader.
        /// </summary>
        protected virtual T? Deserialize<T>(Utf8JsonReader reader)
        {
            return JsonSerializer.Deserialize<T>(ref reader, _readOptions);
        }
    }
}