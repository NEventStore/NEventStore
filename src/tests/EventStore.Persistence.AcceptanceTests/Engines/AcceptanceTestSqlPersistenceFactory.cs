namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using Serialization;
	using SqlPersistence;

	public abstract class AcceptanceTestSqlPersistenceFactory : SqlPersistenceFactory
	{
		protected AcceptanceTestSqlPersistenceFactory(string connectionName)
			: base(connectionName, new BinarySerializer())
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
		public AcceptanceTestAccessPersistenceFactory() : base("Access") { }
	}
	public class AcceptanceTestAmazonRdsPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestAmazonRdsPersistenceFactory() : base("AmazonRDS") { }
	}
	public class AcceptanceTestAzurePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestAzurePersistenceFactory() : base("Azure") { }
	}
	public class AcceptanceTestFirebirdPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestFirebirdPersistenceFactory() : base("Firebird") { }
	}
	public class AcceptanceTestMsSqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestMsSqlPersistenceFactory() : base("MSSQL") { }
	}
	public class AcceptanceTestMySqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestMySqlPersistenceFactory() : base("MySQL") { }
	}
	public class AcceptanceTestPostgreSqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestPostgreSqlPersistenceFactory() : base("PostgreSQL") { }
	}
	public class AcceptanceTestSqlCePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestSqlCePersistenceFactory() : base("SQLCE") { }
	}
	public class AcceptanceTestSqlitePersistenceFactory : AcceptanceTestSqlPersistenceFactory
	{
		public AcceptanceTestSqlitePersistenceFactory() : base("SQLite") { }
	}
}