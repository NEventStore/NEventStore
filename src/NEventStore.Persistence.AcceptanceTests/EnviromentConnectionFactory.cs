namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using NEventStore.Persistence.Sql;

    public class EnviromentConnectionFactory : IConnectionFactory
    {
        private readonly string _envVarKey;
        private readonly DbProviderFactory _dbProviderFactory;

        public EnviromentConnectionFactory(string envDatabaseName, string providerInvariantName)
        {
            _envVarKey = "NEventStore.{0}".FormatWith(envDatabaseName);
            _dbProviderFactory = DbProviderFactories.GetFactory(providerInvariantName);
        }

        public IDbConnection Open()
        {
            return new ConnectionScope("master", OpenInternal);
        }

        public Type GetDbProviderFactoryType()
        {
            return _dbProviderFactory.GetType();
        }

        private IDbConnection OpenInternal()
        {
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
            DbConnection connection = _dbProviderFactory.CreateConnection();
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