namespace NEventStore.Serialization.Bson
{
    /// <summary>
    /// Bson serialization wire-up extensions.
    /// </summary>
    public static class BsonSerializationWireupExtension
    {
        /// <summary>
        /// Specify we want to use Bson serialization.
        /// </summary>
        public static SerializationWireup UsingBsonSerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new BsonSerializer());
        }
    }
}
