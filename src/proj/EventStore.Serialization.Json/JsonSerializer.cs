namespace EventStore.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using NEventStore;
	using NEventStore.Logging;
	using NEventStore.Serialization;
	using Newtonsoft.Json;
	using JsonNetSerializer = Newtonsoft.Json.JsonSerializer;

	public class JsonSerializer : ISerialize
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(JsonSerializer));
		private readonly JsonNetSerializer untypedSerializer = new JsonNetSerializer
		{
			TypeNameHandling = TypeNameHandling.Auto,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore
		};
		private readonly JsonNetSerializer typedSerializer = new JsonNetSerializer
		{
			TypeNameHandling = TypeNameHandling.All,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore
		};
		private readonly IEnumerable<Type> knownTypes = new[]
		{
			typeof(List<EventMessage>),
			typeof(Dictionary<string, object>)
		};

		public JsonSerializer(params Type[] knownTypes)
		{
			if (knownTypes != null && knownTypes.Length == 0)
				knownTypes = null;

			this.knownTypes = knownTypes ?? this.knownTypes;

			foreach (var type in this.knownTypes)
				Logger.Debug(Messages.RegisteringKnownType, type);
		}

		public virtual void Serialize<T>(Stream output, T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));
			using (var streamWriter = new StreamWriter(output, Encoding.UTF8))
				this.Serialize(new JsonTextWriter(streamWriter), graph);
		}
		protected virtual void Serialize(JsonWriter writer, object graph)
		{
			using (writer)
				this.GetSerializer(graph.GetType()).Serialize(writer, graph);
		}

		public virtual T Deserialize<T>(Stream input)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));
			using (var streamReader = new StreamReader(input, Encoding.UTF8))
				return this.Deserialize<T>(new JsonTextReader(streamReader));
		}
		protected virtual T Deserialize<T>(JsonReader reader)
		{
			var type = typeof(T);

			using (reader)
				return (T)this.GetSerializer(type).Deserialize(reader, type);
		}

		protected virtual JsonNetSerializer GetSerializer(Type typeToSerialize)
		{
			if (this.knownTypes.Contains(typeToSerialize))
			{
				Logger.Verbose(Messages.UsingUntypedSerializer, typeToSerialize);
				return this.untypedSerializer;
			}

			Logger.Verbose(Messages.UsingTypedSerializer, typeToSerialize);
			return this.typedSerializer;
		}
	}
}