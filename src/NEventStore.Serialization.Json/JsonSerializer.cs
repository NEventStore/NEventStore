namespace NEventStore.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using NEventStore.Logging;
    using Microsoft.Extensions.Logging;

    public class JsonSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(JsonSerializer));

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

        public JsonSerializer(params Type[] knownTypes)
        {
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = knownTypes ?? _knownTypes;
            
            foreach (var type in _knownTypes)
            {
                Logger.LogDebug(Messages.RegisteringKnownType, type);
            }
        }

        public virtual void Serialize<T>(Stream output, T graph)
        {
            Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            using (var streamWriter = new StreamWriter(output, Encoding.UTF8))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                Serialize(jsonTextWriter, graph);
            }
        }

        public virtual T Deserialize<T>(Stream input)
        {
            Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return Deserialize<T>(jsonTextReader);
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
                Logger.LogTrace(Messages.UsingUntypedSerializer, typeToSerialize);
                return _untypedSerializer;
            }

            Logger.LogTrace(Messages.UsingTypedSerializer, typeToSerialize);
            return _typedSerializer;
        }
    }
}