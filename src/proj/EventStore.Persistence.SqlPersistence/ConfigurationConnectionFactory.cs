namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Linq;

	public class ConfigurationConnectionFactory : IConnectionFactory
	{
		private const int DefaultShards = 16;
		private const string DefaultConnectionName = "EventStore";

		private static readonly IDictionary<string, ConnectionStringSettings> CachedSettings =
			new Dictionary<string, ConnectionStringSettings>();
		private static readonly IDictionary<string, DbProviderFactory> CachedFactories =
			new Dictionary<string, DbProviderFactory>();

		private readonly string masterConnectionName;
		private readonly string replicaConnectionName;
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
			string masterConnectionName, string replicaConnectionName, int shards)
		{
			this.masterConnectionName = masterConnectionName ?? DefaultConnectionName;
			this.replicaConnectionName = replicaConnectionName ?? this.masterConnectionName;
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
			return this.Open(streamId, this.replicaConnectionName);
		}
		protected virtual IDbConnection Open(Guid streamId, string connectionName)
		{
			var setting = this.GetSetting(connectionName);
			var factory = this.GetFactory(setting);
			var connection = factory.CreateConnection();
			if (connection == null)
				throw new ConfigurationErrorsException(Messages.BadConnectionName);

			connection.ConnectionString = this.BuildConnectionString(streamId, setting);

			try
			{
				connection.Open();
			}
			catch (Exception e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}

			return connection;
		}

		protected virtual ConnectionStringSettings GetSetting(string connectionName)
		{
			lock (CachedSettings)
			{
				ConnectionStringSettings setting;
				if (CachedSettings.TryGetValue(connectionName, out setting))
					return setting;

				setting = GetConnectionStringSettings(connectionName);
				return CachedSettings[connectionName] = setting;
			}
		}
		protected virtual DbProviderFactory GetFactory(ConnectionStringSettings setting)
		{
			lock (CachedFactories)
			{
				DbProviderFactory factory;
				if (CachedFactories.TryGetValue(setting.Name, out factory))
					return factory;

				factory = DbProviderFactories.GetFactory(setting.ProviderName);
				return CachedFactories[setting.Name] = factory;
			}
		}
		private static ConnectionStringSettings GetConnectionStringSettings(string connectionName)
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