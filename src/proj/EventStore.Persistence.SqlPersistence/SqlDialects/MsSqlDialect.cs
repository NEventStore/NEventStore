namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class MsSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get { return base.PersistCommitAttempt.Replace(this.Delimiter, string.Empty); }
		}

		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace(this.Delimiter, string.Empty); }
		}
	}
}