namespace NEventStore.Persistence.InMemory
{
    /// <summary>
    ///    Represents a factory for creating in-memory persistence engines.
    /// </summary>
    public class InMemoryPersistenceFactory : IPersistenceFactory
    {
        /// <summary>
        ///    Builds a new in-memory persistence engine.
        /// </summary>
        public virtual IPersistStreams Build()
        {
            return new InMemoryPersistenceEngine();
        }
    }
}