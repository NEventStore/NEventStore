namespace NEventStore.Serialization.Json
{
    public static class JsonSerializationWireupExtension
    {
#if !NETSTANDARD1_6
		public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new BinarySerializer());
        }
#endif

        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            return new SerializationWireup(wireup, serializer);
        }

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
