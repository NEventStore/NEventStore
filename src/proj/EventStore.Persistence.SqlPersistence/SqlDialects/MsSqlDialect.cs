namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data.SqlClient;

	public class MsSqlDialect : CommonSqlDialect
	{
		private const int UniqueKeyViolation = 2627;

		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}
		public override string GetSnapshot
		{
			get { return "SET ROWCOUNT 1;\n" + base.GetSnapshot.Replace("LIMIT 1;", ";"); }
		}

		public override string GetCommitsFromStartingRevision
		{
			get { return Paged(base.GetCommitsFromStartingRevision); }
		}
		public override string GetCommitsFromInstant
		{
			get
			{
				var statement = base.GetCommitsFromInstant.Replace("LIMIT @Limit;", ";");
				var orderByIndex = statement.IndexOf("ORDER BY");
				var orderBy = statement.Substring(orderByIndex).Replace(";", string.Empty);
				statement = statement.Substring(0, orderByIndex);

				var fromIndex = statement.IndexOf("FROM ");
				var from = statement.Substring(fromIndex);
				var select = statement.Substring(0, fromIndex);

				var query = MsSqlStatements.PagedQueryFormat.FormatWith(select, orderBy, from);
				return query;
			}
		}
		public override string GetStreamsRequiringSnapshots
		{
			get { return Paged(base.GetStreamsRequiringSnapshots); }
		}
		private static string Paged(string query)
		{
			return "SET ROWCOUNT @Limit;\n" + query.Replace("LIMIT @Limit;", ";");
		}

		public override bool IsDuplicate(Exception exception)
		{
			var dbException = exception as SqlException;
			return dbException != null && dbException.Number == UniqueKeyViolation;
		}
	}
}