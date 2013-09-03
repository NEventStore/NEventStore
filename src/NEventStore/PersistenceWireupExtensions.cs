namespace NEventStore
{
    using NEventStore.Persistence;
    using NEventStore.Persistence.InMemory;

    public static class PersistenceWireupExtensions
    {
        public static PersistenceWireup UsingInMemoryPersistence(this Wireup wireup)
        {
            wireup.With<IPersistStreams>(new InMemoryPersistenceEngine());

            return new PersistenceWireup(wireup);
        }

        public static int Records(this int records)
        {
            return records;
        }
    }
}