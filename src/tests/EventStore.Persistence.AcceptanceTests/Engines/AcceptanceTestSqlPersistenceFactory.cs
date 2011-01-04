namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using Serialization;
	using SqlPersistence;
	using SqlPersistence.SqlDialects;

	public abstract class AcceptanceTestSqlPersistenceFactory : SqlPersistenceFactory
	{
		protected AcceptanceTestSqlPersistenceFactory(string connectionName, ISqlDialect dialect)
			: base(connectionName, new BinarySerializer(), dialect)
		{
		}

		protected override string TransformConnectionString(string connectionString)
		{
			return connectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore2")
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
	}

	public class AcceptanceTestAccessPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestAccessPersistenceFactory()
			: base("Access", new AccessDialect())
		{
		}
	}
	public class AcceptanceTestAmazonRdsPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestAmazonRdsPersistenceFactory()
			: base("AmazonRDS", new MySqlDialect())
		{
		}
	}
	public class AcceptanceTestAzurePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestAzurePersistenceFactory()
			: base("Azure", new MsSqlDialect())
		{
		}
	}
	public class AcceptanceTestFirebirdPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestFirebirdPersistenceFactory()
			: base("Firebird", new FirebirdSqlDialect())
		{
		}
	}
	public class AcceptanceTestMsSqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestMsSqlPersistenceFactory()
			: base("MSSQL", new MsSqlDialect())
		{
		}
	}
	public class AcceptanceTestMySqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestMySqlPersistenceFactory()
			: base("MySQL", new MySqlDialect())
		{
		}
	}
	public class AcceptanceTestPostgreSqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestPostgreSqlPersistenceFactory()
			: base("PostgreSQL", new PostgreSqlDialect())
		{
		}
	}
	public class AcceptanceTestSqlCePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestSqlCePersistenceFactory()
			: base("SQLCE", new SqlCeDialect())
		{
		}
	}
	public class AcceptanceTestSqlitePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestSqlitePersistenceFactory()
			: base("SQLite", new SqliteDialect())
		{
		}
	}
}