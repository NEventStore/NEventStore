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

        private readonly Newtonsoft.Json.JsonSerializer _typedSerializer;

        private readonly Newtonsoft.Json.JsonSerializer _untypedSerializer;

        /// <summary>
        /// NEventStore Json Serialization using Newtonsoft.Json
        /// </summary>
        /// <param name="jsonSerializerSettings">Allows to configure some Json serialization options,
        /// some of them will be overwritten given the values passed to <paramref name="knownTypes"/> parameter</param>
        /// <param name="knownTypes">
        /// Every Type specified here will be serialized with:
        /// - TypeNameHandling = TypeNameHandling.Auto
        /// - DefaultValueHandling = DefaultValueHandling.Ignore
        /// - NullValueHandling = NullValueHandling.Ignore
        /// Every other type will be serialized with:
        /// - TypeNameHandling = TypeNameHandling.All
        /// - DefaultValueHandling = DefaultValueHandling.Ignore
        /// - NullValueHandling = NullValueHandling.Ignore
        /// </param>
        public JsonSerializer(JsonSerializerSettings jsonSerializerSettings, params Type[] knownTypes)
        {
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = knownTypes ?? _knownTypes;

            _typedSerializer = Newtonsoft.Json.JsonSerializer.Create(jsonSerializerSettings);
            _typedSerializer.TypeNameHandling = TypeNameHandling.All;
            _typedSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            _typedSerializer.NullValueHandling = NullValueHandling.Ignore;

            _untypedSerializer = Newtonsoft.Json.JsonSerializer.Create(jsonSerializerSettings);
            _untypedSerializer.TypeNameHandling = TypeNameHandling.Auto;
            _untypedSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            _untypedSerializer.NullValueHandling = NullValueHandling.Ignore;

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var type in _knownTypes)
                {
                    Logger.LogDebug(Messages.RegisteringKnownType, type);
                }
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