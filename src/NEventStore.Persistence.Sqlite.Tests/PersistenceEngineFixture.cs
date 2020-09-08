namespace NEventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.Sql;
    using NEventStore.Persistence.Sql.SqlDialects;
    using NEventStore.Serialization;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = pageSize => 
                new SqlPersistenceFactory(
                    new ConfigurationConnectionFactory("NEventStore.Persistence.AcceptanceTests.Properties.Settings.SQLite"),
                    new BinarySerializer(),
                    null,
                    new SqliteDialect(),
                    pageSize: pageSize).Build();
        }
    }
}