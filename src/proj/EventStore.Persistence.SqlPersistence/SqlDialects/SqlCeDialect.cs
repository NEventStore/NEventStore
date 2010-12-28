namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class SqlCeDialect : CommonSqlDialect
	{
		private const string Delimiter = ";";

		public override IEnumerable<string> InitializeStorage
		{
			get { return SplitStatement(SqlCeStatements.InitializeStorage); }
		}
		public override IEnumerable<string> AppendSnapshotToCommit
		{
			get { return SplitStatement(base.AppendSnapshotToCommit.First()); }
		}
		public override IEnumerable<string> PersistCommitAttempt
		{
			get { return SplitStatement(base.PersistCommitAttempt.First()); }
		}
		private static IEnumerable<string> SplitStatement(string statement)
		{
			return statement.Split(Delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x + Delimiter);
		}
	}
}