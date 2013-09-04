// ReSharper disable once CheckNamespace
namespace NEventStore
{
    using NEventStore.Persistence.Sql;

    public static class SqlPersistenceWireupExtensions
    {
        public static SqlPersistenceWireup UsingSqlPersistence(this Wireup wireup, string connectionName)
        {
            var factory = new ConfigurationConnectionFactory(connectionName);
            return wireup.UsingSqlPersistence(factory);
        }

        public static SqlPersistenceWireup UsingSqlPersistence(this Wireup wireup, IConnectionFactory factory)
        {
            return new SqlPersistenceWireup(wireup, factory);
        }
    }
}