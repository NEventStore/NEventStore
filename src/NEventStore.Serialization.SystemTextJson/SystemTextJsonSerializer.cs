using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

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
        /// - PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
        /// - SnapshotJsonConverter and ObjectJsonConverter will be added to the Converters collection
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
            using var jsonWriter = new Utf8JsonWriter(output);
            Serialize(jsonWriter, graph);
            jsonWriter.Flush();
        }

        /// <inheritdoc/>
        public virtual T? Deserialize<T>(Stream input)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }

            Type type = typeof(T);
            if (_knownTypes.Contains(type))
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace("Using untyped serializer for type '{Type}'.", type);
                }

                return JsonSerializer.Deserialize<T>(input, _jsonSerializerOptions);
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Using typed serializer for type '{Type}'.", type);
            }

            using var document = JsonDocument.Parse(input);
            return (T?)TypeMetadata.Read(document.RootElement, type, _jsonSerializerOptions);
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

        /// <summary>
        /// Handles reading and writing the Newtonsoft.Json-compatible <c>$type</c> / <c>$values</c> wire format,
        /// so that payloads written by either serializer can be read by the other.
        /// </summary>
        /// <remarks>
        /// <para><b>Wire format</b></para>
        /// <list type="bullet">
        ///   <item><description>Object types: <c>{ "$type": "&lt;AssemblyQualifiedName&gt;", "Prop1": ..., "Prop2": ... }</c></description></item>
        ///   <item><description>Collection types: <c>{ "$type": "&lt;AssemblyQualifiedName&gt;", "$values": [ ... ] }</c></description></item>
        ///   <item><description>Simple/primitive values are written inline without any wrapper.</description></item>
        /// </list>
        /// <para><b>Write path</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="WriteObject"/> — entry point for <see langword="object"/>? slots
        ///   (e.g. <c>EventMessage.Body</c>, <c>Snapshot.Payload</c>, dictionary values).
        ///   Writes null, delegates to the STJ default serializer for simple types, or calls <see cref="Write"/> for everything else.</description></item>
        ///   <item><description><see cref="Write"/> — serialises the value using STJ, then wraps the resulting element in the
        ///   <c>{ $type, ... }</c> envelope. Object elements have their properties inlined;
        ///   array/primitive elements are placed under the <c>$values</c> key.</description></item>
        /// </list>
        /// <para><b>Read path</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="ReadObject"/> — entry point for untyped <see langword="object"/>? slots.
        ///   Dispatches on the JSON value kind: primitives are returned directly, arrays become
        ///   <see cref="List{T}"/> of <see langword="object"/>?, objects are forwarded to <see cref="Read"/> with
        ///   <see cref="Dictionary{TKey,TValue}"/> as the fallback type.</description></item>
        ///   <item><description><see cref="Read"/> — looks for a <c>$type</c> property to determine the concrete type.
        ///   Without <c>$type</c> the element is deserialised as-is using the fallback type. With <c>$type</c>:
        ///   if <c>$values</c> is present the payload is a collection and its array is deserialised directly;
        ///   otherwise <c>$type</c> is stripped from the JSON, nested collection wrappers are optionally normalised
        ///   (see <see cref="NormalizeTypeMetadata"/>), and STJ deserialises the remainder.</description></item>
        /// </list>
        /// <para><b>Normalisation</b></para>
        /// STJ cannot process Newtonsoft-style <c>{ "$type": ..., "$values": [...] }</c> wrappers that appear as
        /// nested property values (e.g. a <see cref="List{T}"/> property serialised with <c>TypeNameHandling.All</c>).
        /// <see cref="NormalizeTypeMetadata"/> recursively walks a <see cref="JsonNode"/> tree and replaces every
        /// <c>{ $type, $values }</c> pair with the unwrapped <c>$values</c> content. It does <b>not</b> strip
        /// <c>$type</c> from plain object nodes — those still carry type information that <c>ObjectJsonConverter</c>
        /// needs to reconstruct polymorphic values. Normalisation is only triggered for <see cref="IDictionary"/>
        /// types (see <see cref="ShouldNormalizeTypeMetadata"/>).
        /// <para><b>Caches</b></para>
        /// <c>TypeNames</c>, <c>ResolvedTypes</c>, <c>SimpleTypes</c>, and <c>DictionaryTypes</c> are
        /// process-wide <see cref="ConcurrentDictionary{TKey,TValue}"/> caches to avoid repeated reflection on hot paths.
        /// </remarks>
        private static class TypeMetadata
        {
            private const string TypePropertyName = "$type";
            private const string ValuesPropertyName = "$values";
            private static readonly ConcurrentDictionary<Type, string> TypeNames = new();
            private static readonly ConcurrentDictionary<string, Type> ResolvedTypes = new();
            private static readonly ConcurrentDictionary<Type, bool> SimpleTypes = new();
            private static readonly ConcurrentDictionary<Type, bool> DictionaryTypes = new();

            /// <summary>
            /// Entry point for writing any <see langword="object"/>? slot whose declared type is <see langword="object"/>.
            /// Simple/primitive values bypass the <c>$type</c> wrapper; complex values go through <see cref="Write"/>.
            /// </summary>
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

            /// <summary>
            /// Wraps a complex value in a <c>{ "$type": "...", &lt;properties&gt; }</c> envelope.
            /// </summary>
            /// <remarks>
            /// STJ first serialises the value normally; the resulting element is then re-emitted under
            /// the <c>$type</c> wrapper. Collections produce a <c>$values</c> array instead of inlined properties.
            /// </remarks>
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
                    // Non-object (array, primitive): store under $values so the read path
                    // can distinguish a collection wrapper from a plain object wrapper.
                    writer.WritePropertyName(ValuesPropertyName);
                    element.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            /// <summary>
            /// Entry point for reading a JSON element whose target type is <see langword="object"/>.
            /// </summary>
            /// <remarks>
            /// Primitive JSON kinds are mapped to their natural CLR types; arrays become
            /// <see cref="List{T}"/> of <see langword="object"/>?; objects are forwarded to
            /// <see cref="Read"/> with a <see cref="Dictionary{TKey,TValue}"/> fallback.
            /// </remarks>
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

            /// <summary>
            /// Reads a JSON element into a typed CLR object, honouring <c>$type</c> metadata.
            /// </summary>
            /// <remarks>
            /// Without <c>$type</c> the <paramref name="fallbackType"/> is used directly.
            /// With <c>$type</c>:
            /// <list type="bullet">
            ///   <item><description><c>$values</c> present — collection wrapper; deserialise the array as the resolved type.</description></item>
            ///   <item><description>No <c>$values</c> — plain object wrapper; remove <c>$type</c>, normalise nested collection
            ///   wrappers for <see cref="IDictionary"/> types, then deserialise the remainder.</description></item>
            /// </list>
            /// </remarks>
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
                    // Collection wrapper: the actual payload lives in $values.
                    return JsonSerializer.Deserialize(valuesProperty.GetRawText(), type, options);
                }

                // Plain object wrapper: strip the outer $type, then hand the remaining JSON to STJ.
                var node = JsonNode.Parse(element.GetRawText())!.AsObject();
                node.Remove(TypePropertyName);
                var json = ShouldNormalizeTypeMetadata(type)
                    ? NormalizeTypeMetadata(node)!.ToJsonString()
                    : node.ToJsonString();
                return JsonSerializer.Deserialize(json, type, options);
            }

            /// <summary>
            /// Recursively unwraps Newtonsoft-style <c>{ "$type": ..., "$values": [...] }</c> collection
            /// wrappers that may appear as property values inside an <see cref="IDictionary"/> payload.
            /// </summary>
            /// <remarks>
            /// Only <c>{ $type + $values }</c> pairs are collapsed — plain <c>{ $type, &lt;properties&gt; }</c>
            /// objects are left intact so that <c>ObjectJsonConverter</c> can still resolve their concrete type.
            /// This preserves polymorphic object-valued dictionary entries while still allowing STJ to
            /// deserialise strongly-typed collection properties (e.g. <see cref="List{T}"/>) that
            /// Newtonsoft wrapped with <c>TypeNameHandling.All</c>.
            /// </remarks>
            private static JsonNode? NormalizeTypeMetadata(JsonNode? node)
            {
                if (node is JsonObject jsonObject)
                {
                    if (jsonObject.TryGetPropertyValue(TypePropertyName, out _) &&
                        jsonObject.TryGetPropertyValue(ValuesPropertyName, out var values))
                    {
                        // Collection wrapper: replace the whole node with its unwrapped content.
                        return NormalizeTypeMetadata(values?.DeepClone());
                    }

                    // Plain object: recurse into each property value.
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

            /// <summary>
            /// Returns the numeric value of a JSON number element, preferring <see langword="long"/> over
            /// <see langword="double"/> so that integer values round-trip without precision loss.
            /// </summary>
            private static object ReadNumber(JsonElement element)
            {
                if (element.TryGetInt64(out var integer))
                {
                    return integer;
                }

                return element.GetDouble();
            }

            /// <summary>
            /// Returns <see langword="true"/> for types that STJ serialises as a plain JSON value,
            /// meaning no <c>$type</c> wrapper is needed.
            /// </summary>
            private static bool IsSimple(Type type)
            {
                return SimpleTypes.GetOrAdd(type, static candidate =>
                {
                    candidate = Nullable.GetUnderlyingType(candidate) ?? candidate;
                    return candidate.IsPrimitive ||
                        candidate.IsEnum ||
                        candidate == typeof(string) ||
                        candidate == typeof(decimal) ||
                        candidate == typeof(Guid) ||
                        candidate == typeof(DateTime) ||
                        candidate == typeof(DateTimeOffset) ||
                        candidate == typeof(TimeSpan);
                });
            }

            /// <summary>
            /// Returns <see langword="true"/> when <paramref name="type"/> implements <see cref="IDictionary"/> —
            /// the only case where <see cref="NormalizeTypeMetadata"/> needs to run.
            /// </summary>
            private static bool ShouldNormalizeTypeMetadata(Type type)
            {
                return DictionaryTypes.GetOrAdd(type, static candidate => typeof(IDictionary).IsAssignableFrom(candidate));
            }

            /// <summary>
            /// Returns the assembly-qualified type name used as the <c>$type</c> value on the wire.
            /// </summary>
            private static string GetTypeName(Type type)
            {
                return TypeNames.GetOrAdd(type, static candidate => candidate.AssemblyQualifiedName ?? candidate.FullName ?? candidate.Name);
            }

            /// <summary>
            /// Resolves a <c>$type</c> string back to a CLR <see cref="Type"/>.
            /// </summary>
            /// <remarks>
            /// Tries <see cref="Type.GetType(string, Func{AssemblyName, Assembly?}, Func{Assembly, string?, bool, Type?}, bool)"/>
            /// first with an assembly resolver that ignores version and public-key token differences.
            /// If that fails, it falls back to scanning loaded assemblies using a generic-aware parser that
            /// strips only the outer assembly qualification from the <c>$type</c> value.
            /// </remarks>
            private static Type ResolveType(string typeName)
            {
                if (ResolvedTypes.TryGetValue(typeName, out var cachedType))
                {
                    return cachedType;
                }

                var type = Type.GetType(typeName, ResolveAssembly, null, throwOnError: false);
                if (type is not null)
                {
                    return ResolvedTypes.GetOrAdd(typeName, type);
                }

                var fullName = GetFullName(typeName);
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName, throwOnError: false);
                    if (type is not null)
                    {
                        return ResolvedTypes.GetOrAdd(typeName, type);
                    }
                }

                throw new JsonException($"Could not resolve serialized type '{typeName}'.");
            }

            /// <summary>
            /// Resolves assemblies by simple name while ignoring version and public-key token differences.
            /// </summary>
            private static Assembly? ResolveAssembly(AssemblyName assemblyName)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var currentAssemblyName = assembly.GetName();
                    if (string.Equals(currentAssemblyName.Name, assemblyName.Name, StringComparison.Ordinal))
                    {
                        return assembly;
                    }
                }

                return null;
            }

            /// <summary>
            /// Strips only the outer assembly token from an assembly-qualified type name.
            /// </summary>
            private static string GetFullName(string typeName)
            {
                var depth = 0;
                for (var index = 0; index < typeName.Length; index++)
                {
                    var character = typeName[index];
                    if (character == '[')
                    {
                        depth++;
                    }
                    else if (character == ']')
                    {
                        if (depth > 0)
                        {
                            depth--;
                        }
                    }
                    else if (character == ',' && depth == 0)
                    {
                        return typeName.Substring(0, index).Trim();
                    }
                }

                return typeName.Trim();
            }
        }
    }
}
