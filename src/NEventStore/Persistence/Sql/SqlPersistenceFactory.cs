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
        private readonly IStreamIdHasher _streamIdHasher;
        private readonly IConnectionFactory _archivingConnection;

        public SqlPersistenceFactory(string connectionName, ISerialize serializer, ISqlDialect dialect = null)
            : this(serializer, TransactionScopeOption.Suppress, null, DefaultPageSize)
        {
            _connectionFactory = new ConfigurationConnectionFactory(connectionName);
            _dialect = dialect ?? ResolveDialect(new ConfigurationConnectionFactory(connectionName).Settings);
        }

        public SqlPersistenceFactory(
            IConnectionFactory factory,
            ISerialize serializer,
            ISqlDialect dialect,
            IStreamIdHasher streamIdHasher = null,
            TransactionScopeOption scopeOption = TransactionScopeOption.Suppress,
            int pageSize = DefaultPageSize,
            IConnectionFactory archivingConnection = null)
            : this(serializer, scopeOption, streamIdHasher, pageSize)
        {
            if (dialect == null)
            {
                throw new ArgumentNullException("dialect");
            }

            _connectionFactory = factory;
            _dialect = dialect;
            _archivingConnection = archivingConnection;
        }

        private SqlPersistenceFactory(ISerialize serializer, TransactionScopeOption scopeOption,  IStreamIdHasher streamIdHasher, int pageSize)
        {
            _serializer = serializer;
            _scopeOption = scopeOption;
            _streamIdHasher = streamIdHasher ?? new Sha1StreamIdHasher();
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

        protected virtual IStreamIdHasher StreamIdHasher
        {
            get { return _streamIdHasher; }
        }

        protected int PageSize { get; set; }

        public virtual IPersistStreams Build()
        {
            return new SqlPersistenceEngine(ConnectionFactory, Dialect, Serializer, _scopeOption, PageSize, StreamIdHasher, _archivingConnection);
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