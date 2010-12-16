namespace EventStore.SqlPersistence.SqlDialects
{
	public class SqliteDialect : CommonSqlDialect
	{
		public override string PersistCommitAttempt
		{
			get { return CommonSqlStatements.SqlitePersistCommitAttempt; }
		}
	}
}