namespace EventStore
{
	using Persistence.MongoPersistence;
	using Serialization;

	public static class MongoPersistenceWireupExtensions
	{
		public static Wireup UsingMongoPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			wireup.With(new MongoPersistenceFactory(connectionName, serializer).Build());
			return wireup;
		}
	}
}