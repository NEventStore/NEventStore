namespace EventStore
{
	using Persistence.RavenPersistence;
	using Serialization;

	public static class RavenPersistenceWireupExtensions
	{
		public static Wireup UsingRavenPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			wireup.With(new RavenPersistenceFactory(connectionName, serializer).Build());
			return wireup;
		}
	}
}