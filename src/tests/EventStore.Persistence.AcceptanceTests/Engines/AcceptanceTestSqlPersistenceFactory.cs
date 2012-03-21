namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System;
	using System.Configuration;
	using System.Transactions;
	using Serialization;
	using SqlPersistence;

	public abstract class AcceptanceTestSqlPersistenceFactory : SqlPersistenceFactory
	{
		protected AcceptanceTestSqlPersistenceFactory(string connectionName)
			: this(new TransformConfigConnectionFactory(connectionName), new BinarySerializer())
		{
		}
		private AcceptanceTestSqlPersistenceFactory(IConnectionFactory factory, ISerialize serializer)
			: base(factory, serializer, null, TransactionScopeOption.Suppress, 0)
		{
			var pageSize = "pageSize".GetSetting();
			if (!string.IsNullOrEmpty(pageSize))
				this.PageSize = int.Parse(pageSize);
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
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore")
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
    public class AcceptanceTestOracleSqlPersistenceFactory : AcceptanceTestSqlPersistenceFactory
    {
        public AcceptanceTestOracleSqlPersistenceFactory() : base("Oracle") { }
    }
}