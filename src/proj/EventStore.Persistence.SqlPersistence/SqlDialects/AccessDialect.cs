namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Text.RegularExpressions;

	public class AccessDialect : CommonSqlDialect
	{
		private const string ParameterPattern = "@[a-z0-9_]+";
		private const string CoalescePattern = @"COALESCE\((?<param>.*?),(?<col>.*?)\)";
		private const string CoalesceReplace = @"''";

		public override string InitializeStorage
		{
			get { return AccessStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get
			{
				var statement = base.PersistCommitAttempt.Replace("/*FROM DUAL*/", "FROM DUAL");
				return Regex.Replace(statement, CoalescePattern, CoalesceReplace);
			}
		}

		public override IDbStatement BuildStatement(IDbConnection connection)
		{
			return new AccessDbStatement(connection);
		}

		private class AccessDbStatement : DelimitedDbStatement
		{
			public AccessDbStatement(IDbConnection connection)
				: base(connection)
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