namespace EventStore.Persistence.AcceptanceTests.SqlPersistence
{
	using Persistence.SqlPersistence;
	using Persistence.SqlPersistence.SqlDialects;

	public class AccessPersistenceFactory : SqlPersistenceFactory
	{
		public override string Name
		{
			get { return "Access"; }
		}
		protected override ISqlDialect BuildDialect()
		{
			return new AccessDialect();
		}
	}
}