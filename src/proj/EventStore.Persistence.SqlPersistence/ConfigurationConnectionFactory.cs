namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Linq;

	public class ConfigurationConnectionFactory : IConnectionFactory
	{
		private const int DefaultShards = 16;
		private const string DefaultConnectionName = "EventStore";
		private readonly string masterConnectionName;
		private readonly string slaveConnectionName;
		private readonly int shards;

		public ConfigurationConnectionFactory()
			: this(null, null, DefaultShards)
		{
		}
		public ConfigurationConnectionFactory(string connectionName)
			: this(connectionName, connectionName, DefaultShards)
		{
		}
		public ConfigurationConnectionFactory(
			string masterConnectionName, string slaveConnectionName, int shards)
		{
			this.masterConnectionName = masterConnectionName ?? DefaultConnectionName;
			this.slaveConnectionName = slaveConnectionName ?? this.masterConnectionName;
			this.shards = shards >= 0 ? shards : DefaultShards;
		}

		public virtual ConnectionStringSettings Settings
		{
			get { return GetConnectionStringSettings(this.masterConnectionName); }
		}

		public virtual IDbConnection OpenMaster(Guid streamId)
		{
			return this.Open(streamId, this.masterConnectionName);
		}
		public virtual IDbConnection OpenReplica(Guid streamId)
		{
			return this.Open(streamId, this.slaveConnectionName);
		}
		protected virtual IDbConnection Open(Guid streamId, string connectionName)
		{
			var setting = GetConnectionStringSettings(connectionName);
			var factory = DbProviderFactories.GetFactory(setting.ProviderName);
			var connection = factory.CreateConnection();
			connection.ConnectionString = this.BuildConnectionString(streamId, setting);
			connection.Open();
			return connection;
		}

		private static ConnectionStringSettings GetConnectionStringSettings(
			string connectionName)
		{
			var settings = ConfigurationManager.ConnectionStrings
				.Cast<ConnectionStringSettings>()
				.FirstOrDefault(x => x.Name == connectionName);

			if (settings == null)
				throw new ConfigurationErrorsException(
					Messages.ConnectionNotFound.FormatWith(connectionName));

			if ((settings.ConnectionString ?? string.Empty).Trim().Length == 0)
				throw new ConfigurationErrorsException(
					Messages.MissingConnectionString.FormatWith(connectionName));

			if ((settings.ProviderName ?? string.Empty).Trim().Length == 0)
				throw new ConfigurationErrorsException(
					Messages.MissingProviderName.FormatWith(connectionName));

			return settings;
		}

		protected virtual string BuildConnectionString(
			Guid streamId, ConnectionStringSettings setting)
		{
			if (this.shards == 0)
				return setting.ConnectionString;

			return setting.ConnectionString.FormatWith(this.ComputeHashKey(streamId));
		}
		protected virtual string ComputeHashKey(Guid streamId)
		{
			// simple sharding scheme which could easily be improved through such techniques
			// as consistent hashing (Amazon Dynamo) or other kinds of sharding.
			return (this.shards == 0 ? 0 : streamId.ToByteArray()[0] % this.shards).ToString();
		}
	}
}