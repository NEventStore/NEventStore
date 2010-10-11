namespace EventStore.Core.IntegrationTests
{
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using SqlStorage;
	using SqlStorage.DynamicSql;
	using SqlStorage.DynamicSql.DialectAdapters;

	public class EventStoreFactory
	{
		private IStoreEvents Build(string connectionStringName)
		{
			var connection = OpenConnection(connectionStringName);
			var commandBuilder = new CommandBuilder(connection);

			var dialect = DiscoverDialect(connection);
			var statementBuilder = new DynamicSqlStatementBuilder(commandBuilder, dialect);
			var storageEngine = new SqlStorageEngine(statementBuilder, new DefaultSerializer());
			return new OptimisticEventStore(storageEngine);
		}

		private static IDbConnection OpenConnection(string connectionStringName)
		{
			var settings = ConfigurationManager.ConnectionStrings[connectionStringName];
			var provider = DbProviderFactories.GetFactory(settings.ProviderName);
			var connection = provider.CreateConnection();
			connection.ConnectionString = settings.ConnectionString;
			connection.Open();
			return connection;
		}
		private static IAdaptDynamicSqlDialect DiscoverDialect(IDbConnection connection)
		{
			var connectType = connection.GetType().FullName;
			if (connectType.Contains("MySql"))
				return new MySqlDialectAdapter();

			if (connectType.Contains("SQLite"))
				return new SqliteDialectAdapter();

			return new MsSqlDialectAdapter();
		}
	}
}