namespace EventStore
{
	using Serialization;

	public static class MongoPersistenceWireupExtensions
	{
        /// <param name="connection">'connectionStrings' config section name or a raw connection string</param>
		public static PersistenceWireup UsingMongoPersistence(
			this Wireup wireup, string connection, IDocumentSerializer serializer)
		{
			return new MongoPersistenceWireup(wireup, connection, serializer);
		}
	}
}