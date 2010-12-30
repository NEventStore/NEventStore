namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	public class DelimitedDbStatement : CommonDbStatement
	{
		private const string Delimiter = ";";

		public DelimitedDbStatement(IDbConnection connection)
			: base(connection)
		{
		}

		public override int Execute(string commandText)
		{
			return SplitCommandText(commandText).Sum(x => this.ExecuteNonQuery(x));
		}
		private static IEnumerable<string> SplitCommandText(string delimited)
		{
			if (string.IsNullOrEmpty(delimited))
				return new string[] { };

			return delimited.Split(Delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.AsEnumerable().Select(x => x + Delimiter)
				.ToArray();
		}
	}
}