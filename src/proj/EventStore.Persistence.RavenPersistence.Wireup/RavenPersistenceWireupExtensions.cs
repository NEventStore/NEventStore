namespace EventStore
{
	using Persistence.RavenPersistence;
	using Serialization;

	public static class RavenPersistenceWireupExtensions
	{
		public static PersistenceWireup UsingRavenPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			var persistence = new RavenPersistenceFactory(connectionName, serializer).Build();
			return new PersistenceWireup(wireup, persistence);
		}
	}
}