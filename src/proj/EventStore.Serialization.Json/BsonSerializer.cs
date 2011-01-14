namespace EventStore.Serialization.Json
{
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Bson;

	public class BsonSerializer : ISerialize
	{
		private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
		{
			TypeNameHandling = TypeNameHandling.Objects
		};

		public void Serialize(Stream output, object graph)
		{
			using (var bsonWriter = new BsonWriter(output))
				this.serializer.Serialize(bsonWriter, graph);
		}
		public object Deserialize(Stream input)
		{
			using (var reader = new BsonReader(input))
				return this.serializer.Deserialize(reader);
		}
	}
}