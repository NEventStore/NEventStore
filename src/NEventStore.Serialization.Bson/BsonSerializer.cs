namespace NEventStore.Serialization.Bson
{
    using System;
    using System.Collections;
    using System.IO;
    using NEventStore.Logging;
    using System.Reflection;
    using Newtonsoft.Json.Bson;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System.Linq;

    public class BsonSerializer : ISerialize
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(BsonSerializer));

        private readonly IEnumerable<Type> _knownTypes = new[] { typeof(List<EventMessage>), typeof(Dictionary<string, object>) };

        private readonly Newtonsoft.Json.JsonSerializer _typedSerializer = new Newtonsoft.Json.JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly Newtonsoft.Json.JsonSerializer _untypedSerializer = new Newtonsoft.Json.JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public BsonSerializer(params Type[] knownTypes)
        {
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = knownTypes ?? _knownTypes;

            if (Logger.IsDebugEnabled)
            {
                foreach (var type in _knownTypes)
                {
                    Logger.Debug(Messages.RegisteringKnownType, type);
                }
            }
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
            Type type = typeof(T);
            return (T)GetSerializer(type).Deserialize(reader, type);
        }

        protected virtual Newtonsoft.Json.JsonSerializer GetSerializer(Type typeToSerialize)
        {
            if (_knownTypes.Contains(typeToSerialize))
            {
                if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.UsingUntypedSerializer, typeToSerialize);
                return _untypedSerializer;
            }

            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.UsingTypedSerializer, typeToSerialize);
            return _typedSerializer;
        }

        private static bool IsArray(Type type)
        {
#if !NETSTANDARD1_6
            bool array = typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type);
#else
            bool array = typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type) && !typeof(IDictionary).GetTypeInfo().IsAssignableFrom(type);
#endif

            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.TypeIsArray, type, array);

            return array;
        }
    }
}