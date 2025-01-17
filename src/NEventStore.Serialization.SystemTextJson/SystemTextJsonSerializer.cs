using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NEventStore.Logging;
using Microsoft.Extensions.Logging;
using NEventStore.Serialization.Json;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// The System.Text.Json JSON serializer.
    /// </summary>
    public class SystemTextJsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(SystemTextJsonSerializer));

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// NEventStore Json Serialization using System.Text.Json
        /// </summary>
        /// <param name="jsonSerializerOptions">
        /// Allows to customize some Serializer options, however some of them will
        /// be under control of this specific implementation and will be overwritten no matter
        /// what the user specifies:
        /// - DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        /// </param>
        public SystemTextJsonSerializer(JsonSerializerOptions? jsonSerializerOptions)
        {
            _jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions();
            _jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
            JsonSerializer.Serialize(writer, graph, _jsonSerializerOptions);
        }

        /// <summary>
        /// Deserialize an object from a Utf8JsonReader.
        /// </summary>
        protected virtual T? Deserialize<T>(Utf8JsonReader reader)
        {
            return JsonSerializer.Deserialize<T>(ref reader, _jsonSerializerOptions);
        }
    }
}
