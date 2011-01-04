namespace EventStore.Persistence
{
	public class InMemoryPersistenceFactory : IPersistenceFactory
	{
		public virtual IPersistStreams Build()
		{
			return new InMemoryPersistenceEngine();
		}
	}
}
