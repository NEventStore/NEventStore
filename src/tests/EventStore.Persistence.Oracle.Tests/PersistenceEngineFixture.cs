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
                    new EnviromentConnectionFactory("Oracle", "Oracle.DataAccess.Client"),
                    new BinarySerializer(),
                    new OracleNativeDialect()).Build();
        }
    }
}