namespace NEventStore.Serialization.Json
{
    public static class JsonSerializationWireupExtension
    {
        public static SerializationWireup UsingJsonSerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new JsonSerializer());
        }
    }
}
