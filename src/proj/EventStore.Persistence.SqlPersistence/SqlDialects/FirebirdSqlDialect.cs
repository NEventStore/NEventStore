namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage; }
		}
		public override string GetCommitsFromSnapshotUntilRevision
		{
			get { return base.GetCommitsFromSnapshotUntilRevision.Replace(".Snapshot", ".\"Snapshot\""); }
		}
		public override string PersistCommitAttempt
		{
			get { return base.PersistCommitAttempt.Replace("/*FROM DUAL*/", "FROM rdb$database"); }
		}
	}
}