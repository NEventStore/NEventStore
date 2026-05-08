using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Serialization.Json;

namespace NEventStore.Serialization.SystemTextJson
{
    /// <summary>
    /// The System.Text.Json JSON serializer.
    /// </summary>
    public class SystemTextJsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(SystemTextJsonSerializer));
        private static readonly Type[] DefaultKnownTypes = [typeof(List<EventMessage>), typeof(Dictionary<string, object>)];

        private readonly HashSet<Type> _knownTypes;
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
        /// <param name="knownTypes">
        /// Every Type specified here will be serialized without root type metadata.
        /// Every other root type and polymorphic object value will be serialized with Newtonsoft-compatible
        /// $type metadata.
        /// </param>
        public SystemTextJsonSerializer(JsonSerializerOptions? jsonSerializerOptions = null, params Type[]? knownTypes)
        {
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = new HashSet<Type>(knownTypes ?? DefaultKnownTypes);
            _jsonSerializerOptions = jsonSerializerOptions is null
                ? new JsonSerializerOptions()
                : new JsonSerializerOptions(jsonSerializerOptions);
            _jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            _jsonSerializerOptions.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
            _jsonSerializerOptions.Converters.Add(new SnapshotJsonConverter());
            _jsonSerializerOptions.Converters.Add(new ObjectJsonConverter(_jsonSerializerOptions));

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var type in _knownTypes)
                {
                    Logger.LogDebug("Registering known type '{Type}'.", type);
                }
            }
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
            var type = graph.GetType();
            if (_knownTypes.Contains(type))
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace("Using untyped serializer for type '{Type}'.", type);
                }

                JsonSerializer.Serialize(writer, graph, type, _jsonSerializerOptions);
                return;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Using typed serializer for type '{Type}'.", type);
            }

            TypeMetadata.Write(writer, graph, type, _jsonSerializerOptions);
        }

        /// <summary>
        /// Deserialize an object from a Utf8JsonReader.
        /// </summary>
        protected virtual T? Deserialize<T>(Utf8JsonReader reader)
        {
            Type type = typeof(T);
            if (_knownTypes.Contains(type))
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace("Using untyped serializer for type '{Type}'.", type);
                }

                return JsonSerializer.Deserialize<T>(ref reader, _jsonSerializerOptions);
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Using typed serializer for type '{Type}'.", type);
            }

            using var document = JsonDocument.ParseValue(ref reader);
            return (T?)TypeMetadata.Read(document.RootElement, type, _jsonSerializerOptions);
        }

        private sealed class ObjectJsonConverter : JsonConverter<object>
        {
            private readonly JsonSerializerOptions _options;

            public ObjectJsonConverter(JsonSerializerOptions options)
            {
                _options = options;
            }

            public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                return TypeMetadata.ReadObject(document.RootElement, _options);
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                TypeMetadata.WriteObject(writer, value, _options);
            }
        }

        private sealed class SnapshotJsonConverter : JsonConverter<Snapshot>
        {
            public override Snapshot? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                var root = document.RootElement;
                var bucketId = root.GetProperty(nameof(Snapshot.BucketId)).GetString();
                var streamId = root.GetProperty(nameof(Snapshot.StreamId)).GetString();
                var streamRevision = root.GetProperty(nameof(Snapshot.StreamRevision)).GetInt32();
                var payload = root.TryGetProperty(nameof(Snapshot.Payload), out var payloadProperty)
                    ? TypeMetadata.ReadObject(payloadProperty, options)
                    : null;

                return new Snapshot(bucketId!, streamId!, streamRevision, payload!);
            }

            public override void Write(Utf8JsonWriter writer, Snapshot value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString(nameof(Snapshot.BucketId), value.BucketId);
                writer.WriteString(nameof(Snapshot.StreamId), value.StreamId);
                writer.WriteNumber(nameof(Snapshot.StreamRevision), value.StreamRevision);
                writer.WritePropertyName(nameof(Snapshot.Payload));
                TypeMetadata.WriteObject(writer, value.Payload, options);
                writer.WriteEndObject();
            }
        }

        private static class TypeMetadata
        {
            private const string TypePropertyName = "$type";
            private const string ValuesPropertyName = "$values";

            public static void WriteObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                var type = value.GetType();
                if (IsSimple(type))
                {
                    JsonSerializer.Serialize(writer, value, type, options);
                    return;
                }

                Write(writer, value, type, options);
            }

            public static void Write(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options)
            {
                var element = JsonSerializer.SerializeToElement(value, type, options);

                writer.WriteStartObject();
                writer.WriteString(TypePropertyName, GetTypeName(type));
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        property.WriteTo(writer);
                    }
                }
                else
                {
                    writer.WritePropertyName(ValuesPropertyName);
                    element.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            public static object? ReadObject(JsonElement element, JsonSerializerOptions options)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => ReadNumber(element),
                    JsonValueKind.Array => JsonSerializer.Deserialize<List<object?>>(element.GetRawText(), options),
                    JsonValueKind.Object => Read(element, typeof(Dictionary<string, object>), options),
                    _ => null,
                };
            }

            public static object? Read(JsonElement element, Type fallbackType, JsonSerializerOptions options)
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty(TypePropertyName, out var typeProperty))
                {
                    return JsonSerializer.Deserialize(element.GetRawText(), fallbackType, options);
                }

                var type = ResolveType(typeProperty.GetString()!);
                if (element.TryGetProperty(ValuesPropertyName, out var valuesProperty))
                {
                    return JsonSerializer.Deserialize(valuesProperty.GetRawText(), type, options);
                }

                var node = JsonNode.Parse(element.GetRawText())!.AsObject();
                node.Remove(TypePropertyName);
                var json = ShouldNormalizeTypeMetadata(type)
                    ? NormalizeTypeMetadata(node)!.ToJsonString()
                    : node.ToJsonString();
                return JsonSerializer.Deserialize(json, type, options);
            }

            private static JsonNode? NormalizeTypeMetadata(JsonNode? node)
            {
                if (node is JsonObject jsonObject)
                {
                    if (jsonObject.TryGetPropertyValue(TypePropertyName, out _))
                    {
                        if (jsonObject.TryGetPropertyValue(ValuesPropertyName, out var values))
                        {
                            return NormalizeTypeMetadata(values?.DeepClone());
                        }

                        jsonObject.Remove(TypePropertyName);
                    }

                    foreach (var property in jsonObject.ToArray())
                    {
                        jsonObject[property.Key] = NormalizeTypeMetadata(property.Value?.DeepClone());
                    }
                }
                else if (node is JsonArray jsonArray)
                {
                    for (var i = 0; i < jsonArray.Count; i++)
                    {
                        jsonArray[i] = NormalizeTypeMetadata(jsonArray[i]?.DeepClone());
                    }
                }

                return node;
            }

            private static object ReadNumber(JsonElement element)
            {
                if (element.TryGetInt64(out var integer))
                {
                    return integer;
                }

                return element.GetDouble();
            }

            private static bool IsSimple(Type type)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
                return type.IsPrimitive ||
                    type.IsEnum ||
                    type == typeof(string) ||
                    type == typeof(decimal) ||
                    type == typeof(Guid) ||
                    type == typeof(DateTime) ||
                    type == typeof(DateTimeOffset) ||
                    type == typeof(TimeSpan);
            }

            private static bool ShouldNormalizeTypeMetadata(Type type)
            {
                return typeof(IDictionary).IsAssignableFrom(type);
            }

            private static string GetTypeName(Type type)
            {
                return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            }

            private static Type ResolveType(string typeName)
            {
                var type = Type.GetType(typeName, throwOnError: false);
                if (type is not null)
                {
                    return type;
                }

                var fullName = typeName.Split(',')[0].Trim();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName, throwOnError: false);
                    if (type is not null)
                    {
                        return type;
                    }
                }

                throw new JsonException($"Could not resolve serialized type '{typeName}'.");
            }
        }
    }
}
