namespace EventStore
{
	using Serialization;

	public static class SerializationWireupExtensions
	{
		public static SerializationWireup UsingBinarySerialization(this Wireup wireup)
		{
			return wireup.UsingCustomSerializer(new BinarySerializer());
		}

		public static SerializationWireup UsingCustomSerializer(this Wireup wireup, ISerialize serializer)
		{
			return new SerializationWireup(wireup, serializer);
		}
	}
}