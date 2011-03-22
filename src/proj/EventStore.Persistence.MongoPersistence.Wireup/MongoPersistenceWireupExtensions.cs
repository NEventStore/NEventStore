namespace EventStore
{
	using Persistence.MongoPersistence;
	using Serialization;

	public static class MongoPersistenceWireupExtensions
	{
		public static Wireup UsingRavenPersistence(
			this Wireup wireup, string connectionName, ISerialize serializer)
		{
			wireup.With(new MongoPersistenceFactory(connectionName, serializer).Build());
			return wireup;
		}
	}
}