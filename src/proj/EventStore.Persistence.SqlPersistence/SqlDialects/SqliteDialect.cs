namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class SqliteDialect : CommonSqlDialect
	{
		public override string PersistCommitAttempt
		{
			get { return SqliteStatements.PersistCommitAttempt; }
		}
	}
}