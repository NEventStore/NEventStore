namespace NEventStore.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using NEventStore.Logging;

    public class JsonSerializer : ISerialize
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(JsonSerializer));

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
            if (knownTypes != null && knownTypes.Length == 0)
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
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.SerializingGraph, typeof(T));
            using (var streamWriter = new StreamWriter(output, Encoding.UTF8))
                Serialize(new JsonTextWriter(streamWriter), graph);
        }

        public virtual T Deserialize<T>(Stream input)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.DeserializingStream, typeof(T));
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
                return Deserialize<T>(new JsonTextReader(streamReader));
        }

        protected virtual void Serialize(JsonWriter writer, object graph)
        {
            using (writer)
                GetSerializer(graph.GetType()).Serialize(writer, graph);
        }

        protected virtual T Deserialize<T>(JsonReader reader)
        {
            Type type = typeof(T);

            using (reader)
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
    }
}