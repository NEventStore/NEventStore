namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;
	using Persistence.SqlPersistence;
	using Serialization;

	public abstract class SqlPersistenceFactory : IPersistenceFactory
	{
		public abstract string Name { get; }

		public virtual IPersistStreams Build()
		{
			return new SqlPersistenceEngine(
				new DelegateConnectionFactory(id => this.OpenConnection()),
				this.BuildDialect(),
				new BinarySerializer());
		}
		private IDbConnection OpenConnection()
		{
			var setting = ConfigurationManager.ConnectionStrings[this.Name];
			var factory = DbProviderFactories.GetFactory(setting.ProviderName);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = BuildConnectionString(setting);
			connection.Open();
			return connection;
		}
		private static string BuildConnectionString(ConnectionStringSettings setting)
		{
			return setting.ConnectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore2")
				.Replace("[USERNAME]", "username".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}

		protected abstract ISqlDialect BuildDialect();
	}
}