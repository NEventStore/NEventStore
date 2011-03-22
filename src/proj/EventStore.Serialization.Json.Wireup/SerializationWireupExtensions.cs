namespace EventStore
{
	using Serialization;

	public static class WireupExtensions
	{
		public static SerializationWireup UsingJsonSerialization(this Wireup wireup)
		{
			return wireup.UsingCustomSerialization(new JsonSerializer());
		}

		public static SerializationWireup UsingBsonSerialization(this Wireup wireup)
		{
			return wireup.UsingCustomSerialization(new BsonSerializer());
		}
	}
}