namespace EventStore
{
	using Persistence.SqlPersistence;

	public static class SqlPersistenceWireupExtensions
	{
		public static SqlPersistenceWireup UsingSqlPersistence(this Wireup wireup, string connectionName)
		{
			var factory = new ConfigurationConnectionFactory(connectionName);
			return wireup.UsingSqlPersistence(factory);
		}
		public static SqlPersistenceWireup UsingSqlPersistence(this Wireup wireup, IConnectionFactory factory)
		{
			return new SqlPersistenceWireup(wireup, factory);
		}
	}
}