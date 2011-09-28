namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Transactions;

	public class AccessDialect : CommonSqlDialect
	{
		private const string ParameterPattern = "@[a-z0-9_]+";

		public override string InitializeStorage
		{
			get { return AccessStatements.InitializeStorage; }
		}
		public override string PersistCommit
		{
			get { return base.PersistCommit.Replace("/*FROM DUAL*/", "FROM DUAL"); }
		}
		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace("/*FROM DUAL*/", "FROM DUAL"); }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT TOP 1 *").Replace("LIMIT 1", string.Empty); }
		}
		public override string GetStreamsRequiringSnapshots
		{
			get { return RemovePaging(AccessStatements.GetStreamsToSnapshot); }
		}
		public override string GetCommitsFromInstant
		{
			get { return RemovePaging(base.GetCommitsFromInstant); }
		}
		public override string GetCommitsFromStartingRevision
		{
			get { return RemovePaging(base.GetCommitsFromStartingRevision); }
		}
		public override string GetUndispatchedCommits
		{
			get { return RemovePaging(base.GetUndispatchedCommits); }
		}
		private static string RemovePaging(string query)
		{
			return query.Replace("LIMIT @Limit;", ";");
		}

		public override bool CanPage
		{
			get { return false; }
		}

		public override object CoalesceParameterValue(object value)
		{
			if (value is DateTime)
				return (decimal)((DateTime)value).Ticks;

			return value;
		}

		public override IDbStatement BuildStatement(
			TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
		{
			return new AccessDbStatement(this, scope, connection, transaction);
		}

		private class AccessDbStatement : DelimitedDbStatement
		{
			public AccessDbStatement(
				ISqlDialect dialect,
				TransactionScope scope,
				IDbConnection connection,
				IDbTransaction transaction)
				: base(dialect, scope, connection, transaction)
			{
			}

			protected override void BuildParameters(IDbCommand command)
			{
				foreach (var item in this.Parameters.Where(item => item.Value is int))
					command.CommandText = command.CommandText.Replace(item.Key, item.Value.ToString());

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