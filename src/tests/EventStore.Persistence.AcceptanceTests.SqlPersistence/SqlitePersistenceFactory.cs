namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class SqlitePersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "SQLite"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new SqliteDialect();
		}
	}
}