using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using EventStore.Persistence;
using EventStore.Serialization;

namespace EventStore.SqlPersistence.AcceptanceTests
{
	public static class TestSqlPersistenceFactory
	{
		public static IPersistStreams CreateSqlPersistence(string connectionName) {
			return  new SqlPersistence(
				new DelegateConnectionFactory(id => OpenConnection(connectionName)),
				new BinarySerializer());
		}

		private static IDbConnection OpenConnection(string connectionName)
		{
			var setting = ConfigurationManager.ConnectionStrings[connectionName];
			var factory = DbProviderFactories.GetFactory(setting.ProviderName);
			var connection = factory.CreateConnection() ?? new SqlConnection();
			connection.ConnectionString = setting.ConnectionString;
			connection.Open();
			return connection;
		}
	}
}