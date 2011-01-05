namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Text.RegularExpressions;

	public class AccessDialect : CommonSqlDialect
	{
		private const string ParameterPattern = "@[a-z0-9_]+";

		public override string InitializeStorage
		{
			get { return AccessStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get
			{
				return base.PersistCommitAttempt
					.Replace("/*FROM DUAL*/", "FROM DUAL")
					.Replace(this.CommitStamp, "now()");
			}
		}
		public override string GetStreamsRequiringSnaphots
		{
			get { return AccessStatements.GetStreamsRequiringSnapshots; }
		}
		public override string AppendSnapshotToCommit
		{
			get { return AccessStatements.AppendSnapshot; }
		}

		public override IDbTransaction OpenTransaction(IDbConnection connection)
		{
			return connection.BeginTransaction(IsolationLevel.ReadUncommitted);
		}
		public override IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction)
		{
			return new AccessDbStatement(connection, transaction);
		}

		private class AccessDbStatement : DelimitedDbStatement
		{
			public AccessDbStatement(IDbConnection connection, IDbTransaction transaction)
				: base(connection, transaction)
			{
			}

			protected override void BuildParameters(IDbCommand command)
			{
				// parameter names are resolved based upon their order, not name
				foreach (var name in DiscoverParameters(command.CommandText))
					this.BuildParameter(command, name, this.Parameters[name]);
			}
			private static IEnumerable<string> DiscoverParameters(string statement)
			{
				if (string.IsNullOrEmpty(statement))
					return new string[] { };

				var matches = Regex.Matches(statement, ParameterPattern, RegexOptions.IgnoreCase);
				return from Match match in matches select match.Value; // non-unique
			}
		}
	}
}