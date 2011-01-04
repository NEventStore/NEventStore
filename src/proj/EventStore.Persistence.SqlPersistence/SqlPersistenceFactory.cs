namespace EventStore.Persistence.SqlPersistence
{
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;
	using Serialization;

	public abstract class SqlPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		protected SqlPersistenceFactory(string connectionName, ISqlDialect dialect, ISerialize serializer)
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
				new DelegateConnectionFactory(id => this.OpenConnection()),
				this.dialect,
				this.serializer);
		}
		protected virtual IDbConnection OpenConnection()
		{
			var setting = ConfigurationManager.ConnectionStrings[this.Name];
			var factory = DbProviderFactories.GetFactory(setting.ProviderName);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = this.TransformConnectionString(setting.ConnectionString);
			connection.Open();
			return connection;
		}
		protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
	}
}