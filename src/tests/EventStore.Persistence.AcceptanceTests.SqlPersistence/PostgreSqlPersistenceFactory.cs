namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class PostgreSqlPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "PostgreSQL"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new PostgreSqlDialect();
		}
	}
}