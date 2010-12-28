namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class SqlCePersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "SQLCE"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new SqlCeDialect();
		}
	}
}