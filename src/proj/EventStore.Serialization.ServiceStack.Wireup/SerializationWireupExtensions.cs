namespace EventStore
{
	using Serialization;

	public static class WireupExtensions
	{
		public static SerializationWireup UsingServiceStackJsonSerialization(this PersistenceWireup wireup)
		{
			return wireup.UsingCustomSerialization(new ServiceStackJsonSerializer());
		}
	}
}