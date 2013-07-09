namespace NEventStore
{
    using NEventStore;
    using NEventStore.Serialization;

    public static class MongoPersistenceWireupExtensions
	{
		public static PersistenceWireup UsingMongoPersistence(
			this Wireup wireup, string connectionName, IDocumentSerializer serializer)
		{
			return new MongoPersistenceWireup(wireup, connectionName, serializer);
		}
	}
}