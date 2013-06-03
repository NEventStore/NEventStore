namespace EventStore.Persistence.AcceptanceTests
{
    using System.Transactions;
    using Serialization;
    using SqlPersistence;
    using SqlPersistence.SqlDialects;

    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.CreatePersistence = () => 
                new SqlPersistenceFactory(
                    new ConfigurationConnectionFactory("name"),
                    new BinarySerializer(),
                    new SqlCeDialect(),
                    TransactionScopeOption.Suppress,
                    10).Build();
        }
    }
}