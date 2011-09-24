namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Transactions;

	public class DelimitedDbStatement : CommonDbStatement
	{
		private const string Delimiter = ";";

		public DelimitedDbStatement(
			ISqlDialect dialect,
			TransactionScope transactionScope,
			ConnectionScope connectionScope,
			IDbTransaction transaction)
			: base(dialect, transactionScope, connectionScope, transaction)
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