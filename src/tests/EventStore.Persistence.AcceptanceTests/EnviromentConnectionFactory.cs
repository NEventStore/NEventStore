namespace EventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using NEventStore;
    using NEventStore.Persistence;
    using NEventStore.Persistence.SqlPersistence;

    public class EnviromentConnectionFactory : IConnectionFactory
    {
        private readonly string providerInvariantName;
        private readonly string envVarKey;

        public EnviromentConnectionFactory(string envDatabaseName, string providerInvariantName)
        {
            this.envVarKey = "NEventStore.{0}".FormatWith(envDatabaseName);
            this.providerInvariantName = providerInvariantName;
        }

        public IDbConnection OpenMaster(Guid streamId)
        {
            return new ConnectionScope("master", Open);
        }

        public IDbConnection OpenReplica(Guid streamId)
        {
            return new ConnectionScope("master", Open);
        }

        private IDbConnection Open()
        {
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(providerInvariantName);
            string connectionString = Environment.GetEnvironmentVariable(envVarKey, EnvironmentVariableTarget.Process);
            if (connectionString == null)
            {
                string message = string.Format("Failed to get '{0}' environment variable. Please ensure " +
                    "you have correctly setup the connection string environment variables. Refer to the " +
                    "NEventStore wiki for details.", envVarKey);
                throw new InvalidOperationException(message);
            }
            connectionString = connectionString.TrimStart('"').TrimEnd('"');
            DbConnection connection = dbProviderFactory.CreateConnection();
            Debug.Assert(connection != null, "connection != null");
            connection.ConnectionString = connectionString;
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
    }
}