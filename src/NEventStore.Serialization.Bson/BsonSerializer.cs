using System.Collections;
using NEventStore.Logging;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace NEventStore.Serialization.Bson
{
    /// <summary>
    /// Represents a BSON serializer.
    /// </summary>
    public class BsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(BsonSerializer));

        private readonly IEnumerable<Type> _knownTypes = [typeof(List<EventMessage>), typeof(Dictionary<string, object>)];

        private readonly JsonSerializer _typedSerializer = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly JsonSerializer _untypedSerializer = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Initializes a new instance of the BsonSerializer class.
        /// </summary>
        public BsonSerializer(params Type[]? knownTypes)
        {
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = knownTypes ?? _knownTypes;

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var type in _knownTypes)
                {
                    Logger.LogDebug(Messages.RegisteringKnownType, type);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Serialize<T>(Stream output, T graph) where T: notnull
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }
            using var writer = new BsonDataWriter(output) { DateTimeKindHandling = DateTimeKind.Utc };
            Serialize(writer, graph);
        }

        /// <inheritdoc/>
        public virtual T? Deserialize<T>(Stream input)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }
            using var reader = new BsonDataReader(input, IsArray(typeof(T)), DateTimeKind.Utc);
            return Deserialize<T>(reader);
        }

        /// <summary>
        /// Serialize an object to a JsonWriter.
        /// </summary>
        protected virtual void Serialize(JsonWriter writer, object graph)
        {
            GetSerializer(graph.GetType()).Serialize(writer, graph);
        }

        /// <summary>
        /// Deserialize an object from a JsonReader.
        /// </summary>
        protected virtual T? Deserialize<T>(JsonReader reader)
        {
            Type type = typeof(T);
            return (T?)GetSerializer(type).Deserialize(reader, type);
        }

        /// <summary>
        /// Get the serializer for the given type.
        /// </summary>
        protected virtual Newtonsoft.Json.JsonSerializer GetSerializer(Type typeToSerialize)
        {
            if (_knownTypes.Contains(typeToSerialize))
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(Messages.UsingUntypedSerializer, typeToSerialize);
                }
                return _untypedSerializer;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.UsingTypedSerializer, typeToSerialize);
            }
            return _typedSerializer;
        }

        private static bool IsArray(Type type)
        {
            bool array = typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type);

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.TypeIsArray, type, array);
            }

            return array;
        }
    }
}