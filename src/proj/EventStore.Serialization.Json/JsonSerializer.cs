namespace EventStore.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using Newtonsoft.Json;
	using JsonNetSerializer = Newtonsoft.Json.JsonSerializer;

	public class JsonSerializer : ISerialize
	{
		private readonly JsonNetSerializer untypedSerializer = new JsonNetSerializer
		{
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

		public JsonSerializer()
			: this(null)
		{
		}
		public JsonSerializer(params Type[] knownTypes)
		{
			this.knownTypes = knownTypes ?? this.knownTypes;
		}

		public virtual void Serialize(Stream output, object graph)
		{
			if (graph == null)
				return;

			using (var streamWriter = new StreamWriter(output, Encoding.UTF8))
			using (var writer = new JsonTextWriter(streamWriter))
				this.GetSerializer(graph.GetType()).Serialize(writer, graph);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			using (var streamReader = new StreamReader(input, Encoding.UTF8))
			using (var reader = new JsonTextReader(streamReader))
				return (T)this.GetSerializer(typeof(T)).Deserialize(reader, typeof(T));
		}
		protected virtual JsonNetSerializer GetSerializer(Type typeToSerialize)
		{
			return this.knownTypes.Contains(typeToSerialize)
				? this.untypedSerializer : this.typedSerializer;
		}
	}
}