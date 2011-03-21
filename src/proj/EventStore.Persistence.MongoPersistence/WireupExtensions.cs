namespace EventStore
{
	using Persistence.MongoPersistence;
	using Serialization;

	public static class WireupExtensions
	{
		public static IWireup UsingRavenPersistence(
			this IWireup wireup, string connectionName, ISerialize serializer)
		{
			wireup.With(new MongoPersistenceFactory(connectionName, serializer).Build());
			return wireup;
		}
	}
}