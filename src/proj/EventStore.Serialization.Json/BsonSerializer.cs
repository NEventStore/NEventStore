namespace EventStore.Serialization
{
	using System;
	using System.IO;
	using Newtonsoft.Json.Bson;

	public class BsonSerializer : JsonSerializer
	{
		public BsonSerializer()
		{
		}
		public BsonSerializer(params Type[] knownTypes)
			: base(knownTypes)
		{
		}

		public override void Serialize(Stream output, object graph)
		{
			if (graph == null)
				return;

			using (var writer = new BsonWriter(output))
				this.GetSerializer(graph).Serialize(writer, graph);
		}
		public override T Deserialize<T>(Stream input)
		{
			using (var reader = new BsonReader(input))
				return (T)this.GetSerializer(null).Deserialize(reader, typeof(T));
		}
	}
}