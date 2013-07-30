namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using NEventStore.Persistence.SqlPersistence;

    public class EnviromentConnectionFactory : IConnectionFactory
    {
        private readonly string _envVarKey;
        private readonly string _providerInvariantName;

        public EnviromentConnectionFactory(string envDatabaseName, string providerInvariantName)
        {
            _envVarKey = "NEventStore.{0}".FormatWith(envDatabaseName);
            _providerInvariantName = providerInvariantName;
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
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(_providerInvariantName);
            string connectionString = Environment.GetEnvironmentVariable(_envVarKey, EnvironmentVariableTarget.Process);
            if (connectionString == null)
            {
                string message =
                    string.Format(
                                  "Failed to get '{0}' environment variable. Please ensure " +
                                      "you have correctly setup the connection string environment variables. Refer to the " +
                                      "NEventStore wiki for details.",
                        _envVarKey);
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