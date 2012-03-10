using System;
using System.Configuration;
using System.Data.Common;
using EventStore.Persistence.SqlPersistence;

namespace EventStore.Persistence.AcceptanceTests.Engines
{
    public class OracleAwareConfigurationConnectionFactory : ConfigurationConnectionFactory
    {
        private const string version = "2.102.2.20";
        private const string assemblyNameTemplate = "Oracle.DataAccess,Version={0},Culture=neutral,PublicKeyToken=89b483f429c47342";
        private const string clientFactoryType = "Oracle.DataAccess.Client.OracleClientFactory";

        public OracleAwareConfigurationConnectionFactory(string connectionName)
            : base(connectionName)
        {
        }

        public OracleAwareConfigurationConnectionFactory(string masterConnectionName, string replicaConnectionName, int shards) : base(masterConnectionName, replicaConnectionName, shards)
        {
        }

        protected override DbProviderFactory GetClientFactory(ConnectionStringSettings setting)
        {
            if(!setting.ProviderName.StartsWith("Oracle"))
                return base.GetFactory(setting);

            var fullyQualifiedAssemblyName = string.Format(assemblyNameTemplate, getVersion());
            var name = string.Format("{0}, {1}", clientFactoryType, fullyQualifiedAssemblyName);
            var clientType = Type.GetType(name);
            var factory = (DbProviderFactory)Activator.CreateInstance(clientType);
            return factory;
        }

        private string getVersion()
        {
            return version;
        }
    }
}