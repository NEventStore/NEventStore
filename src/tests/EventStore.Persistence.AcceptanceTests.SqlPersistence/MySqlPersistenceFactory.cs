namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class MySqlPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "MySQL"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new MySqlDialect();
		}
	}
}