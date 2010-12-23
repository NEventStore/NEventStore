namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class AzurePersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "Azure"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new MsSqlDialect();
		}
	}
}