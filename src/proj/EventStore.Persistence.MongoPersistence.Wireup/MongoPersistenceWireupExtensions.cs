namespace EventStore
{
	using Persistence.MongoPersistence;
	using Serialization;

	public static class MongoPersistenceWireupExtensions
	{
		public static PersistenceWireup UsingMongoPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			var persistence = new MongoPersistenceFactory(connectionName, serializer).Build();
			return new PersistenceWireup(wireup, persistence);
		}
	}
}