namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using NEventStore.Persistence.Sql.SqlDialects;
    using NEventStore.Serialization;

    public class SqlPersistenceFactory : IPersistenceFactory
    {
        private const int DefaultPageSize = 128;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ISqlDialect _dialect;
        private readonly TransactionScopeOption _scopeOption;
        private readonly ISerialize _serializer;

        public SqlPersistenceFactory(string connectionName, ISerialize serializer, ISqlDialect dialect = null)
            : this(serializer, TransactionScopeOption.Suppress, DefaultPageSize)
        {
            _connectionFactory = new ConfigurationConnectionFactory(connectionName);
            _dialect = dialect ?? ResolveDialect(new ConfigurationConnectionFactory(connectionName).Settings);
        }

        public SqlPersistenceFactory(
            IConnectionFactory factory,
            ISerialize serializer,
            ISqlDialect dialect,
            TransactionScopeOption scopeOption = TransactionScopeOption.Suppress,
            int pageSize = DefaultPageSize)
            : this(serializer, scopeOption, pageSize)
        {
            if (dialect == null)
            {
                throw new ArgumentNullException("dialect");
            }

            _connectionFactory = factory;
            _dialect = dialect;
        }

        private SqlPersistenceFactory(ISerialize serializer, TransactionScopeOption scopeOption, int pageSize)
        {
            _serializer = serializer;
            _scopeOption = scopeOption;

            PageSize = pageSize;
        }

        protected virtual IConnectionFactory ConnectionFactory
        {
            get { return _connectionFactory; }
        }

        protected virtual ISqlDialect Dialect
        {
            get { return _dialect; }
        }

        protected virtual ISerialize Serializer
        {
            get { return _serializer; }
        }

        protected int PageSize { get; set; }

        public virtual IPersistStreams Build()
        {
            return new SqlPersistenceEngine(
                ConnectionFactory, Dialect, Serializer, _scopeOption, PageSize);
        }

        protected static ISqlDialect ResolveDialect(ConnectionStringSettings settings)
        {
            string providerName = settings.ProviderName.ToUpperInvariant();

            if (providerName.Contains("MYSQL"))
            {
                return new MySqlDialect();
            }

            if (providerName.Contains("SQLITE"))
            {
                return new SqliteDialect();
            }

            if (providerName.Contains("POSTGRES") || providerName.Contains("NPGSQL"))
            {
                return new PostgreSqlDialect();
            }

            if (providerName.Contains("ORACLE") && providerName.Contains("DATAACCESS"))
            {
                return new OracleNativeDialect();
            }

            if (providerName == "SYSTEM.DATA.ORACLECLIENT")
            {
                return new OracleNativeDialect();
            }

            return new MsSqlDialect();
        }
    }
}