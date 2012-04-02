namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Transactions;
	using Serialization;
	using SqlDialects;

	public class SqlPersistenceFactory : IPersistenceFactory
	{
		private const int DefaultPageSize = 128;
		private readonly IConnectionFactory connectionFactory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;
		private readonly TransactionScopeOption scopeOption;

		public SqlPersistenceFactory(string connectionName, ISerialize serializer)
			: this(connectionName, serializer, null)
		{
		}
		public SqlPersistenceFactory(string connectionName, ISerialize serializer, ISqlDialect dialect)
			: this(serializer, TransactionScopeOption.Suppress, DefaultPageSize)
		{
			this.connectionFactory = new ConfigurationConnectionFactory(connectionName);
			this.dialect = dialect ?? ResolveDialect(new ConfigurationConnectionFactory(connectionName).Settings);
		}
		public SqlPersistenceFactory(IConnectionFactory factory, ISerialize serializer, ISqlDialect dialect)
			: this(factory, serializer, dialect, TransactionScopeOption.Suppress, DefaultPageSize)
		{
		}
		public SqlPersistenceFactory(
			IConnectionFactory factory,
			ISerialize serializer,
			ISqlDialect dialect,
			TransactionScopeOption scopeOption,
			int pageSize)
			: this(serializer, scopeOption, pageSize)
		{
			if (dialect == null)
				throw new ArgumentNullException("dialect");

			this.connectionFactory = factory;
			this.dialect = dialect;
		}
		private SqlPersistenceFactory(ISerialize serializer, TransactionScopeOption scopeOption, int pageSize)
		{
			this.serializer = serializer;
			this.scopeOption = scopeOption;

			this.PageSize = pageSize;
		}

		protected virtual IConnectionFactory ConnectionFactory
		{
			get { return this.connectionFactory; }
		}
		protected virtual ISqlDialect Dialect
		{
			get { return this.dialect; }
		}
		protected virtual ISerialize Serializer
		{
			get { return this.serializer; }
		}
		protected int PageSize { get; set; }

		public virtual IPersistStreams Build()
		{
			return new SqlPersistenceEngine(
				this.ConnectionFactory, this.Dialect, this.Serializer, this.scopeOption, this.PageSize);
		}

		protected static ISqlDialect ResolveDialect(ConnectionStringSettings settings)
		{
			var connectionString = settings.ConnectionString.ToUpperInvariant();
			var providerName = settings.ProviderName.ToUpperInvariant();

			if (providerName.Contains("MYSQL"))
				return new MySqlDialect();

			if (providerName.Contains("SQLITE"))
				return new SqliteDialect();

			if (providerName.Contains("SQLSERVERCE") || connectionString.Contains(".SDF"))
				return new SqlCeDialect();

			if (providerName.Contains("FIREBIRD"))
				return new FirebirdSqlDialect();

			if (providerName.Contains("POSTGRES") || providerName.Contains("NPGSQL"))
				return new PostgreSqlDialect();

			if (providerName.Contains("FIREBIRD"))
				return new FirebirdSqlDialect();

			if (providerName.Contains("OLEDB") && connectionString.Contains("MICROSOFT.JET"))
				return new AccessDialect();

			return new MsSqlDialect();
		}
	}
}