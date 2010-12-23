namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class AmazonRdsPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "AmazonRDS"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new MySqlDialect();
		}
	}
}