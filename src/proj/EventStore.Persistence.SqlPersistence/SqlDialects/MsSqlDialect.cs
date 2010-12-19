namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class MsSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}
	}
}