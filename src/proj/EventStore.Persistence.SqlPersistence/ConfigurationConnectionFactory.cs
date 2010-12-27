namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	public class ConfigurationConnectionFactory : IConnectionFactory
	{
		private const string DefaultConnectionName = "EventStore";
		private const string DefaultProvider = "System.Data.SqlClient";
		private readonly string connectionName;

		public ConfigurationConnectionFactory()
			: this(null)
		{
		}
		public ConfigurationConnectionFactory(string connectionName)
		{
			this.connectionName = connectionName ?? DefaultConnectionName;
		}

		public virtual IDbConnection Open(Guid streamId)
		{
			var setting = ConfigurationManager.ConnectionStrings[this.connectionName];
			var factory = DbProviderFactories.GetFactory(setting.ProviderName ?? DefaultProvider);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = this.BuildConnectionString(streamId, setting);
			connection.Open();
			return connection;
		}
		protected virtual string BuildConnectionString(Guid streamId, ConnectionStringSettings setting)
		{
			// streamId is used if we want to vary the connection based upon some kind of sharding strategy.
			return setting.ConnectionString;
		}
	}
}