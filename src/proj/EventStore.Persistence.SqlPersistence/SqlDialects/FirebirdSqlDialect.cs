namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage; }
		}
	}
}