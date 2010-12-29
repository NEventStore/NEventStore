namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	internal static class ExtensionMethods
	{
		private const string Delimiter = ";";

		public static IEnumerable<string> SplitStatement(this string statement)
		{
			if (string.IsNullOrEmpty(statement))
				return new string[] { };

			return statement.Split(Delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x + Delimiter);
		}
	}
}