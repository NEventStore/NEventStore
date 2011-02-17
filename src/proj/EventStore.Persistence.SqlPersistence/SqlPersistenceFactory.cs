namespace EventStore.Persistence.SqlPersistence
{
	using Serialization;
	using SqlDialects;

	public class SqlPersistenceFactory : IPersistenceFactory
	{
		private readonly IConnectionFactory connectionFactory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlPersistenceFactory(string connectionName, ISerialize serializer)
			: this(connectionName, serializer, null)
		{
		}
		public SqlPersistenceFactory(string connectionName, ISerialize serializer, ISqlDialect dialect)
			: this(new ConfigurationConnectionFactory(connectionName), serializer, dialect)
		{
		}
		public SqlPersistenceFactory(IConnectionFactory factory, ISerialize serializer)
			: this(factory, serializer, null)
		{
		}
		public SqlPersistenceFactory(IConnectionFactory factory, ISerialize serializer, ISqlDialect dialect)
		{
			this.connectionFactory = factory;
			this.serializer = serializer;
			this.dialect = dialect;
		}

		public virtual IPersistStreams Build()
		{
			return new SqlPersistenceEngine(this.connectionFactory, this.GetDialect(), this.serializer);
		}
		protected virtual ISqlDialect GetDialect()
		{
			if (this.dialect != null)
				return this.dialect;

			var settings = this.connectionFactory.Settings;
			var connectionString = (settings.ConnectionString ?? string.Empty).ToUpperInvariant();
			var providerName = (settings.ProviderName ?? string.Empty).ToUpperInvariant();

			if (providerName.Contains("MYSQL"))
				return new MySqlDialect();

			if (providerName.Contains("SQLITE"))
				return new SqliteDialect();

			if (providerName.Contains("SQLSERVERCE"))
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