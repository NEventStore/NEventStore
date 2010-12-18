namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class SqliteDialect : CommonSqlDialect
	{
		public override string PersistCommitAttempt
		{
			get { return SqliteStatements.PersistCommitAttempt; }
		}
		public override string AppendSnapshotToCommit
		{
			get { return SqliteStatements.AppendSnapshotToCommit; }
		}
	}
}