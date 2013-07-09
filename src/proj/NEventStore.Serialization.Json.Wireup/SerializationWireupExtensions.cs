namespace NEventStore
{
    using NEventStore.Serialization;

    public static class WireupExtensions
	{
		public static SerializationWireup UsingJsonSerialization(this PersistenceWireup wireup)
		{
			return wireup.UsingCustomSerialization(new JsonSerializer());
		}

		public static SerializationWireup UsingBsonSerialization(this PersistenceWireup wireup)
		{
			return wireup.UsingCustomSerialization(new BsonSerializer());
		}
	}
}