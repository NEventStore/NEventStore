namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;
	using System.Linq;

	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage.SplitStatement(); }
		}
		public override IEnumerable<string> AppendSnapshotToCommit
		{
			get
			{
				return base.AppendSnapshotToCommit
					.First()
					.SplitStatement()
					.Select(statement => statement.Replace("Snapshot ", "\"Snapshot\" "));
			}
		}
		public override string GetCommitsFromSnapshotUntilRevision
		{
			get { return base.GetCommitsFromSnapshotUntilRevision.Replace(".Snapshot", ".\"Snapshot\""); }
		}
		public override IEnumerable<string> PersistCommitAttempt
		{
			get
			{
				return base.PersistCommitAttempt
					.First()
					.SplitStatement()
					.Select(statement => statement.Replace("/*FROM DUAL*/", "FROM rdb$database"));
			}
		}
	}
}