namespace EventStore
{
	using Serialization;

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
	}
}