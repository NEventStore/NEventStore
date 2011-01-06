namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get { return base.PersistCommitAttempt.Replace("/*FROM DUAL*/", "FROM rdb$database"); }
		}
		public override string GetCommitsFromSnapshotUntilRevision
		{
			get { return base.GetCommitsFromSnapshotUntilRevision.Replace(".Snapshot", ".\"Snapshot\""); }
		}
		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace("Snapshot ", "\"Snapshot\" "); }
		}
		public override string GetStreamsRequiringSnaphots
		{
			get { return base.GetStreamsRequiringSnaphots.Replace("Snapshot ", "\"Snapshot\" "); }
		}

		public override IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction)
		{
			return new FirebirdDbStatement(connection, transaction);
		}

		private class FirebirdDbStatement : DelimitedDbStatement
		{
			public FirebirdDbStatement(IDbConnection connection, IDbTransaction transaction)
				: base(connection, transaction)
			{
			}
		}
	}
}