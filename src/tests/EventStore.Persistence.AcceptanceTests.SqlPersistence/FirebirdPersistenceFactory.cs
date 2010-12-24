namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class FirebirdPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "Firebird"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new FirebirdSqlDialect();
		}
	}
}