namespace EventStore.Core.IntegrationTests
{
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using SqlStorage;
	using SqlStorage.DynamicSql;
	using SqlStorage.DynamicSql.DialectAdapters;

	public class EventStoreFactory
	{
		public static IEnumerable<IStoreEvents> ForEach()
		{
			foreach (ConnectionStringSettings settings in ConfigurationManager.ConnectionStrings)
			{
				var connection = OpenConnection(settings);
				var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

				yield return Build(connection, transaction);

				transaction.Dispose();
				connection.Dispose();
			}
		}
		private static IDbConnection OpenConnection(ConnectionStringSettings settings)
		{
			var provider = DbProviderFactories.GetFactory(settings.ProviderName);
			var connection = provider.CreateConnection();
			connection.ConnectionString = settings.ConnectionString;
			connection.Open();
			return connection;
		}
		private static IStoreEvents Build(IDbConnection connection, IDbTransaction transaction)
		{
			var commandBuilder = new CommandBuilder(connection, transaction);
			var dialect = DiscoverDialect(connection);
			var statementBuilder = new DynamicSqlStatementBuilder(commandBuilder, dialect);
			var storageEngine = new SqlStorageEngine(statementBuilder, new DefaultSerializer());
			return new OptimisticEventStore(storageEngine);
		}
		private static IAdaptDynamicSqlDialect DiscoverDialect(IDbConnection connection)
		{
			// TODO: execute DDL scripts to pre-populate the schema?
			var connectType = connection.GetType().FullName;
			if (connectType.Contains("MySql"))
				return new MySqlDialectAdapter();

			if (connectType.Contains("SQLite"))
				return new SqliteDialectAdapter();

			return new MsSqlDialectAdapter();
		}
	}
}