namespace EventStore
{
	using Persistence;

	public static class PersistenceWireupExtensions
	{
		public static PersistenceWireup UsingInMemoryPersistence(this Wireup wireup)
		{
			wireup.With<IPersistStreams>(new InMemoryPersistenceEngine());

			return new PersistenceWireup(wireup);
		}
	}
}