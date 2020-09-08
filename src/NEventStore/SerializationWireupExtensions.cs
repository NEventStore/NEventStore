namespace NEventStore
{
    using NEventStore.Serialization;

    public static class SerializationWireupExtensions
    {
        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new BinarySerializer());
        }

        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            return new SerializationWireup(wireup, serializer);
        }

        public static SnapshotSerializationWireup UsingCustomSnapshotSerialization(this PersistenceWireup wireup, ISerializeSnapshots serializer)
        {
            return new SnapshotSerializationWireup(wireup, serializer);
        }

        public static SnapshotSerializationWireup UsingCustomSnapshotSerialization(this SerializationWireup wireup, ISerializeSnapshots serializer)
        {
            return new SnapshotSerializationWireup(wireup, serializer);
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