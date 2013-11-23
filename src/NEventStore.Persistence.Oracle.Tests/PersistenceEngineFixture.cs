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
                    new EnviromentConnectionFactory("Oracle", "Oracle.DataAccess.Client"),
                    new BinarySerializer(),
                    new OracleNativeDialect(),
                    pageSize: pageSize).Build();
        }
    }
}