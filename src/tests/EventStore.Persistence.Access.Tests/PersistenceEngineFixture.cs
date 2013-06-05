namespace EventStore.Persistence.AcceptanceTests
{
    using Serialization;
    using SqlPersistence;
    using SqlPersistence.SqlDialects;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.CreatePersistence = () => 
                new SqlPersistenceFactory(
                    new ConfigurationConnectionFactory("EventStore.Persistence.AcceptanceTests.Properties.Settings.Access"),
                    new BinarySerializer(),
                    new AccessDialect()).Build();
        }
    }
}