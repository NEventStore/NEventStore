namespace EventStore.SqlPersistence.SqlDialects
{
	public class MySqlDialect : CommonSqlDialect
	{
		public override string AppendSnapshotToCommit
		{
			get { return MySqlStatements.AppendSnapshotToCommit; }
		}
		public override string PersistCommitAttempt
		{
			get { return MySqlStatements.PersistCommitAttempt; }
		}
	}
}