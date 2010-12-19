namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class SqliteDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqliteStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get { return SqliteStatements.PersistCommitAttempt; }
		}
	}
}