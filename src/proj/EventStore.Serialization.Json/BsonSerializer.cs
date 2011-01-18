namespace EventStore.Serialization
{
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Bson;

	public class BsonSerializer : ISerialize
	{
		private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
		{
			TypeNameHandling = TypeNameHandling.All,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore
		};

		public void Serialize(Stream output, object graph)
		{
			using (var writer = new BsonWriter(output))
				this.serializer.Serialize(writer, graph);
		}
		public object Deserialize(Stream input)
		{
			using (var reader = new BsonReader(input))
				return this.serializer.Deserialize(reader);
		}
	}
}