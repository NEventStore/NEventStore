namespace EventStore
{
	using Serialization;

	public static class WireupExtensions
	{
		public static SerializationWireup UsingServiceStackJsonSerialization(this Wireup wireup)
		{
			return wireup.UsingCustomSerialization(new ServiceStackJsonSerializer());
		}
	}
}