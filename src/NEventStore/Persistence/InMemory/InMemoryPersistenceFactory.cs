namespace NEventStore.Persistence.InMemory
{
    public class InMemoryPersistenceFactory : IPersistenceFactory
    {
        public virtual IPersistStreams Build()
        {
            return new InMemoryPersistenceEngine();
        }
    }
}