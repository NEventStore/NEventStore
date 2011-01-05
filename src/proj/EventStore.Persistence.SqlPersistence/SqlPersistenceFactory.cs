namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;
	using Serialization;
	using SqlDialects;

	public class SqlPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlPersistenceFactory(string connectionName, ISerialize serializer)
			: this(connectionName, serializer, null)
		{
		}
		public SqlPersistenceFactory(string connectionName, ISerialize serializer, ISqlDialect dialect)
		{
			this.connectionName = connectionName;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		protected virtual string Name
		{
			get { return this.connectionName; }
		}

		public virtual IPersistStreams Build()
		{
			return new SqlPersistenceEngine(
				new DelegateConnectionFactory(this.OpenConnection), this.GetDialect(), this.serializer);
		}
		protected virtual IDbConnection OpenConnection(Guid streamId)
		{
			var settings = this.GetConnectionSettings(streamId);
			var factory = DbProviderFactories.GetFactory(settings.ProviderName);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = this.TransformConnectionString(settings.ConnectionString);
			connection.Open();
			return connection;
		}
		protected virtual ConnectionStringSettings GetConnectionSettings(Guid streamId)
		{
			// streamId allows use to change the connection based upon some kind of sharding strategy.
			return ConfigurationManager.ConnectionStrings[this.Name];
		}
		protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
		protected virtual ISqlDialect GetDialect()
		{
			if (this.dialect != null)
				return this.dialect;

			var settings = this.GetConnectionSettings(Guid.Empty);
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