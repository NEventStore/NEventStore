namespace EventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using SqlPersistence;

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