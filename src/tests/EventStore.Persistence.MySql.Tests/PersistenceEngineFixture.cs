namespace EventStore.Persistence.AcceptanceTests
{
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.SqlPersistence;
    using NEventStore.Persistence.SqlPersistence.SqlDialects;
    using NEventStore.Serialization;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.createPersistence = () =>
                new SqlPersistenceFactory(
                    new EnviromentConnectionFactory("MySql", "MySql.Data.MySqlClient"),
                    new BinarySerializer(),
                    new MySqlDialect()).Build();
        }
    }
}