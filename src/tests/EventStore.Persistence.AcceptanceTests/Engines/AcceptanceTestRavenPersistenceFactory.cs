using EventStore.Persistence.RavenPersistence;
using EventStore.Serialization;

namespace EventStore.Persistence.AcceptanceTests.Engines
{
    public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
    {
        public AcceptanceTestRavenPersistenceFactory()
            : base("RavenDB", new BinarySerializer())
        {
        }
    }
}