namespace EventStore
{
	using Serialization;

	public static class RavenPersistenceWireupExtensions
	{
		public static PersistenceWireup UsingRavenPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			return wireup.UsingRavenPersistence(connectionName, serializer, false);
		}
		public static PersistenceWireup UsingRavenPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer, bool consistentQueries)
		{
			return new RavenPersistenceWireup(wireup, connectionName, serializer, consistentQueries);
		}
	}
}