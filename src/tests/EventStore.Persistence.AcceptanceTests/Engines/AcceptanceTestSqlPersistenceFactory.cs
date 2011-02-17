namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System;
	using System.Configuration;
	using Serialization;
	using SqlPersistence;

	public abstract class AcceptanceTestSqlPersistenceFactory : SqlPersistenceFactory
	{
		protected AcceptanceTestSqlPersistenceFactory(string connectionName)
			: base(new TransformConfigConnectionFactory(connectionName), new BinarySerializer())
		{
		}
	}

	public class TransformConfigConnectionFactory : ConfigurationConnectionFactory
	{
		public TransformConfigConnectionFactory(string connectionName)
			: base(connectionName)
		{
		}

		protected override string BuildConnectionString(Guid streamId, ConnectionStringSettings setting)
		{
			return setting.ConnectionString
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