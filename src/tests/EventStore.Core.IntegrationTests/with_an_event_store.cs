// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Transactions;
	using Machine.Specifications;
	using SqlStorage;
	using SqlStorage.DynamicSql;
	using SqlStorage.DynamicSql.DialectAdapters;

	public abstract class with_an_event_store : open_a_connection
	{
		protected static IStoreEvents store;

		Establish context = () =>
		{
			var commandBuilder = new CommandBuilder(connection, null);
			var dialect = DiscoverDialect();
			var statementBuilder = new DynamicSqlStatementBuilder(commandBuilder, dialect, Guid.NewGuid());
			var storageEngine = new SqlStorageEngine(statementBuilder, new DefaultSerializer());
			store = new OptimisticEventStore(storageEngine);
		};

		private static IAdaptDynamicSqlDialect DiscoverDialect()
		{
			switch (connectionName)
			{
				case "MySQL": return new MySqlDialectAdapter();
				case "SQLite": return new SqliteDialectAdapter();
				case "SQL Server": return new MsSqlDialectAdapter();
			}

			throw new NotSupportedException();
		}
	}

	public abstract class open_a_connection : within_a_transaction
	{
		protected static string connectionName = "SQLite"; // default
		protected static IDbConnection connection;

		Establish content = () =>
		{
			AppDomain.CurrentDomain.SetData("DataDirectory", Environment.CurrentDirectory);
			var settings = ConfigurationManager.ConnectionStrings[connectionName];
			var provider = DbProviderFactories.GetFactory(settings.ProviderName);
			connection = provider.CreateConnection();
			connection.ConnectionString = settings.ConnectionString;
			connection.Open();
		};

		Cleanup after = () =>
		{
			if (null != connection)
				connection.Dispose();
		};
	}

	public abstract class within_a_transaction
	{
		// static TransactionScope scope;
		// Establish context = () => scope = new TransactionScope();
		// Cleanup after = () => scope.Dispose();
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming