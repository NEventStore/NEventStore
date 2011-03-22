namespace EventStore
{
	using Serialization;

	public static class SerializationWireupExtensions
	{
		public static SerializationWireup UsingBinarySerialization(this Wireup wireup)
		{
			return wireup.UsingCustomSerialization(new BinarySerializer());
		}

		public static SerializationWireup UsingCustomSerialization(this Wireup wireup, ISerialize serializer)
		{
			return new SerializationWireup(wireup, serializer);
		}
	}
}