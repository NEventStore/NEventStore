namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class PostgreSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return PostgreSqlStatements.InitializeStorage; }
		}
	}
}