namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class MsSqlPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "MSSQL"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new MsSqlDialect();
		}
	}
}