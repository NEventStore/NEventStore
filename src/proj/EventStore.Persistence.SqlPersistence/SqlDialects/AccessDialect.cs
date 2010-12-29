namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;
	using System.Linq;

	public class AccessDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { return AccessStatements.InitializeStorage.SplitStatement(); }
		}
		public override IEnumerable<string> AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.First().SplitStatement(); }
		}
		public override IEnumerable<string> PersistCommitAttempt
		{
			get
			{
				return base.PersistCommitAttempt
					.First()
					.SplitStatement()
					.Select(statement => statement.Replace("/*FROM DUAL*/", "FROM DUAL"));
			}
		}
	}
}