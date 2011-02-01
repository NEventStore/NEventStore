namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	public class ConfigurationConnectionFactory : IConnectionFactory
	{
		private const int DefaultShards = 16;
		private const string DefaultConnectionName = "EventStore";
		private const string DefaultProvider = "System.Data.SqlClient";
		private readonly string readConnectionName;
		private readonly string writeConnectionName;
		private readonly int shards;

		public ConfigurationConnectionFactory()
			: this(null, null, DefaultShards)
		{
		}
		public ConfigurationConnectionFactory(string connectionName)
			: this(connectionName, connectionName, DefaultShards)
		{
		}
		public ConfigurationConnectionFactory(string readConnectionName, string writeConnectionName, int shards)
		{
			this.readConnectionName = readConnectionName ?? DefaultConnectionName;
			this.writeConnectionName = writeConnectionName ?? this.readConnectionName;
			this.shards = shards >= 0 ? shards : DefaultShards;
		}

		public virtual IDbConnection OpenForReading(Guid streamId)
		{
			return this.Open(streamId, this.readConnectionName);
		}
		public virtual IDbConnection OpenForWriting(Guid streamId)
		{
			return this.Open(streamId, this.writeConnectionName);
		}
		protected virtual IDbConnection Open(Guid streamId, string connectionName)
		{
			var setting = ConfigurationManager.ConnectionStrings[connectionName];
			var factory = DbProviderFactories.GetFactory(setting.ProviderName ?? DefaultProvider);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = this.BuildConnectionString(streamId, setting);
			connection.Open();
			return connection;
		}
		protected virtual string BuildConnectionString(Guid streamId, ConnectionStringSettings setting)
		{
			if (this.shards == 0)
				return setting.ConnectionString;

			return setting.ConnectionString.FormatWith(this.ComputeShardKey(streamId));
		}
		protected virtual int ComputeShardKey(Guid streamId)
		{
			return this.shards == 0 ? 0 : streamId.ToByteArray()[0] % this.shards;
		}
	}
}