namespace EventStore.Serialization
{
	using System;
	using System.IO;
	using MongoDB.Bson;
	using MongoDB.Bson.IO;
	using MongoDB.Bson.Serialization;

	public class MongoSerializer : ISerialize
	{
		public void Serialize(Stream output, object graph)
		{
			using (var writer = BsonWriter.Create(output))
			{
				var data = BsonBinaryData.Create(graph);
				writer.WriteBinaryData(data.Bytes, BsonBinarySubType.Binary);
			}
		}

		public object Deserialize(Stream input)
		{
			using (var reader = BsonReader.Create(input))
			{
				return BsonSerializer.Deserialize<object>(reader);
			}
		}
	}
}
