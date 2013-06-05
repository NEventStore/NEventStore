namespace EventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using SqlPersistence;

    public class EnviromentConnectionFactory : IConnectionFactory
    {
        public IDbConnection OpenMaster(Guid streamId)
        {
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
            return new ConnectionScope("master", () => Open("MySQL", dbProviderFactory));
        }

        public IDbConnection OpenReplica(Guid streamId)
        {
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
            return new ConnectionScope("master", () => Open("MySQL", dbProviderFactory));
        }

        private IDbConnection Open(string envVar, DbProviderFactory factory)
        {
            string envVarKey = "NEventStore:{0}".FormatWith(envVar);
            string connectionString = Environment.GetEnvironmentVariable(envVarKey, EnvironmentVariableTarget.Process);
            connectionString = connectionString.TrimStart('"').TrimEnd('"');
            DbConnection connection = factory.CreateConnection();
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