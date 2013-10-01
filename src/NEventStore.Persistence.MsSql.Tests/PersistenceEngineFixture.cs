namespace NEventStore.Persistence.AcceptanceTests
{
    using System.Transactions;
    using NEventStore.Persistence.Sql;
    using NEventStore.Persistence.Sql.SqlDialects;
    using NEventStore.Serialization;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = () =>
                new SqlPersistenceFactory(new EnviromentConnectionFactory("MsSql", "System.Data.SqlClient"),
                    new BinarySerializer(),
                    new MsSqlDialect(),
                    TransactionScopeOption.Required).Build();
        }
    }
}