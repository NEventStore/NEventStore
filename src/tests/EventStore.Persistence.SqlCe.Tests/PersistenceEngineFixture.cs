namespace EventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.SqlPersistence;
    using NEventStore.Persistence.SqlPersistence.SqlDialects;
    using NEventStore.Serialization;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () => 
                new SqlPersistenceFactory(
                    new ConfigurationConnectionFactory("EventStore.Persistence.AcceptanceTests.Properties.Settings.SQLCE"),
                    new BinarySerializer(),
                    new SqlCeDialect()).Build();
        }
    }
}