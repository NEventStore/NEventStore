namespace EventStore.Persistence.MongoPersistence
{
	using System.IO;
	using MongoDB.Bson;
	using MongoDB.Bson.IO;
	using MongoDB.Bson.Serialization;
	using Serialization;

	public class MongoSerializer : ISerialize
	{
		public virtual void Serialize<T>(Stream output, T graph)
		{
			using (var writer = BsonWriter.Create(output))
			{
				var data = BsonBinaryData.Create(graph);
				writer.WriteBinaryData(data.Bytes, BsonBinarySubType.Binary);
			}
		}
		public virtual T Deserialize<T>(Stream input)
		{
			using (var reader = BsonReader.Create(input))
				return BsonSerializer.Deserialize<T>(reader);
		}
	}
}