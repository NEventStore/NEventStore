namespace NEventStore.Serialization.Bson
{
    public static class BsonSerializationWireupExtension
    {
        public static SerializationWireup UsingBsonSerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new BsonSerializer());
        }
    }
}