using EventStore.Persistence.AcceptanceTests.Engines;

namespace EventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.CreatePersistence = () => 
                new AcceptanceTestSqlitePersistenceFactory().Build();

            PurgeOnDispose = true;
        }
    }
}