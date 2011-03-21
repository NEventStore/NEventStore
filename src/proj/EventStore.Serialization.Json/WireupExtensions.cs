namespace EventStore
{
	using Serialization;

	public static class WireupExtensions
	{
		public static IWireup UsingJsonSerialization(this IWireup wireup)
		{
			wireup.With<ISerialize>(new JsonSerializer());
			return wireup;
		}

		public static IWireup UsingBsonSerialization(this IWireup wireup)
		{
			wireup.With<ISerialize>(new BsonSerializer());
			return wireup;
		}
	}
}