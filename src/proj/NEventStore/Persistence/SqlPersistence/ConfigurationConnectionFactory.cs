namespace EventStore.Persistence.SqlPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using Logging;

    public class ConfigurationConnectionFactory : IConnectionFactory
	{
		private const int DefaultShards = 16;
		private const string DefaultConnectionName = "EventStore";

		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ConfigurationConnectionFactory));
		private static readonly IDictionary<string, ConnectionStringSettings> CachedSettings =
			new Dictionary<string, ConnectionStringSettings>();
		private static readonly IDictionary<string, DbProviderFactory> CachedFactories =
			new Dictionary<string, DbProviderFactory>();

		private readonly string masterConnectionName;
		private readonly string replicaConnectionName;
		private readonly int shards;

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

			Logger.Debug(Messages.ConfiguringConnections,
				this.masterConnectionName, this.replicaConnectionName, this.shards);
		}

		public static IDisposable OpenScope()
		{
			var settings = CachedSettings.FirstOrDefault();
			if (string.IsNullOrEmpty(settings.Key))
				throw new ConfigurationErrorsException(Messages.NotConnectionsAvailable);

			return OpenScope(Guid.Empty, settings.Key);
		}
		public static IDisposable OpenScope(string connectionName)
		{
			return OpenScope(Guid.Empty, connectionName);
		}
		public static IDisposable OpenScope(Guid streamId, string connectionName)
		{
			var factory = new ConfigurationConnectionFactory(connectionName);
			return factory.Open(streamId, connectionName);
		}

		public virtual ConnectionStringSettings Settings
		{
			get { return this.GetConnectionStringSettings(this.masterConnectionName); }
		}

		public virtual IDbConnection OpenMaster(Guid streamId)
		{
			Logger.Verbose(Messages.OpeningMasterConnection, this.masterConnectionName);
			return this.Open(streamId, this.masterConnectionName);
		}
		public virtual IDbConnection OpenReplica(Guid streamId)
		{
			Logger.Verbose(Messages.OpeningReplicaConnection, this.replicaConnectionName);
			return this.Open(streamId, this.replicaConnectionName);
		}
		protected virtual IDbConnection Open(Guid streamId, string connectionName)
		{
			var setting = this.GetSetting(connectionName);
			var connectionString = this.BuildConnectionString(streamId, setting);
			return new ConnectionScope(connectionString, () => this.Open(connectionString, setting));
		}
		protected virtual IDbConnection Open(string connectionString, ConnectionStringSettings setting)
		{
			var factory = this.GetFactory(setting);
			var connection = factory.CreateConnection();
			if (connection == null)
				throw new ConfigurationErrorsException(Messages.BadConnectionName);

			connection.ConnectionString = connectionString;

			try
			{
				Logger.Verbose(Messages.OpeningConnection, setting.Name);
				connection.Open();
			}
			catch (Exception e)
			{
				Logger.Warn(Messages.OpenFailed, setting.Name);
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

				setting = this.GetConnectionStringSettings(connectionName);
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
				Logger.Debug(Messages.DiscoveredConnectionProvider, setting.Name, factory.GetType());
				return CachedFactories[setting.Name] = factory;
			}
		}
		protected virtual ConnectionStringSettings GetConnectionStringSettings(string connectionName)
		{
			Logger.Debug(Messages.DiscoveringConnectionSettings, connectionName);

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

		protected virtual string BuildConnectionString(Guid streamId, ConnectionStringSettings setting)
		{
			if (this.shards == 0)
				return setting.ConnectionString;

			Logger.Verbose(Messages.EmbeddingShardKey, setting.Name);
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