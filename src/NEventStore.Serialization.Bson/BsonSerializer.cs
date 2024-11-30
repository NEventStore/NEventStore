namespace NEventStore.Serialization.Bson
{
    using System;
    using System.Collections;
    using System.IO;
    using Logging;
    using System.Reflection;
    using Newtonsoft.Json.Bson;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    public class BsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(BsonSerializer));

        private readonly IEnumerable<Type> _knownTypes = new[]
            { typeof(List<EventMessage>), typeof(Dictionary<string, object>) };

        private readonly JsonSerializer _typedSerializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly JsonSerializer _untypedSerializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public BsonSerializer(params Type[] knownTypes)
        {
            if (knownTypes?.Length == 0) knownTypes = null;

            _knownTypes = knownTypes ?? _knownTypes;

            if (Logger.IsEnabled(LogLevel.Debug))
                foreach (var type in _knownTypes)
                    Logger.LogDebug(Messages.RegisteringKnownType, type);
        }

        public virtual void Serialize<T>(Stream output, T graph)
        {
            using (var writer = new BsonDataWriter(output) { DateTimeKindHandling = DateTimeKind.Utc })
            {
                Serialize(writer, graph);
            }
        }

        public virtual T Deserialize<T>(Stream input)
        {
            using (var reader = new BsonDataReader(input, IsArray(typeof(T)), DateTimeKind.Utc))
            {
                return Deserialize<T>(reader);
            }
        }

        protected virtual void Serialize(JsonWriter writer, object graph)
        {
            GetSerializer(graph.GetType()).Serialize(writer, graph);
        }

        protected virtual T Deserialize<T>(JsonReader reader)
        {
            var type = typeof(T);
            return (T)GetSerializer(type).Deserialize(reader, type);
        }

        protected virtual JsonSerializer GetSerializer(Type typeToSerialize)
        {
            if (_knownTypes.Contains(typeToSerialize))
            {
                Logger.LogTrace(Messages.UsingUntypedSerializer, typeToSerialize);
                return _untypedSerializer;
            }

            Logger.LogTrace(Messages.UsingTypedSerializer, typeToSerialize);
            return _typedSerializer;
        }

        private static bool IsArray(Type type)
        {
            var array = typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type);

            Logger.LogTrace(Messages.TypeIsArray, type, array);

            return array;
        }
    }
}