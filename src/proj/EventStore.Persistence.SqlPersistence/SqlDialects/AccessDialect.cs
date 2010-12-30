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
		//private const string CoalesceReplace = @"iif(isnull(${param}), ${col}, ${param})";
		private const string CoalesceReplace = @"''";

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
			get { return base.PersistCommitAttempt.First().SplitStatement().Select(ToAccessStatement); }
		}
		private static string ToAccessStatement(string statement)
		{
			statement = statement.Replace("/*FROM DUAL*/", "FROM DUAL");
			return Regex.Replace(statement, CoalescePattern, CoalesceReplace);
		}

		public override void AmmendStatement(IDbCommand command)
		{
			var parameters = GetParameters(command);

			AddParametersInStatementOrder(command, parameters);
			AddAllParameters(command, parameters);
		}
		private static IDictionary<string, object> GetParameters(IDbCommand command)
		{
			var parameters = new Dictionary<string, object>(command.Parameters.Count);

			foreach (IDataParameter parameter in command.Parameters)
				parameters[parameter.ParameterName] = parameter.Value;

			return parameters;
		}
		private static void AddParametersInStatementOrder(
			IDbCommand command, IDictionary<string, object> parameters)
		{
			command.Parameters.Clear();

			var parameterInStatement = GetParameterNames(command.CommandText);

			foreach (var parameterName in parameterInStatement)
				command.AddParameter(parameterName, parameters[parameterName]);
		}
		private static IEnumerable<string> GetParameterNames(string statement)
		{
			if (string.IsNullOrEmpty(statement))
				return new string[] { };

			var matches = Regex.Matches(statement, ParameterPattern, RegexOptions.IgnoreCase);
			return from Match match in matches select match.Value; // non-unique
		}
		private static void AddAllParameters(
			IDbCommand command, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			foreach (var parameter in parameters)
				command.AddParameter(parameter.Key, parameter.Value);
		}
	}
}