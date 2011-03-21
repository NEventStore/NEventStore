namespace EventStore
{
	using Persistence.RavenPersistence;
	using Serialization;

	public static class WireupExtensions
	{
		public static IWireup UsingRavenPersistence(
			this IWireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			wireup.With(new RavenPersistenceFactory(connectionName, serializer).Build());
			return wireup;
		}
	}
}