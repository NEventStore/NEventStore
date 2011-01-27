namespace EventStore.Persistence
{
	public class InMemoryPersistenceFactory : IPersistenceFactory
	{
		private static readonly IPersistStreams Engine = new InMemoryPersistenceEngine();

		public virtual IPersistStreams Build()
		{
			return Engine;
		}
	}
}
