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
			get { return NaturalPaging(base.GetCommitsFromStartingRevision); }
		}
		public override string GetCommitsFromInstant
		{
			get { return CommonTableExpressionPaging(base.GetCommitsFromInstant); }
		}
		public override string GetCommitsFromToInstant
		{
			get { return CommonTableExpressionPaging(base.GetCommitsFromToInstant); }
		}
		public override string GetUndispatchedCommits
		{
			get { return CommonTableExpressionPaging(base.GetUndispatchedCommits); }
		}
		public override string GetStreamsRequiringSnapshots
		{
			get { return NaturalPaging(base.GetStreamsRequiringSnapshots); }
		}

		private static string NaturalPaging(string query)
		{
			return "SET ROWCOUNT @Limit;\n" + RemovePaging(query);
		}
		private static string CommonTableExpressionPaging(string query)
		{
			query = RemovePaging(query);
			var orderByIndex = query.IndexOf("ORDER BY");
			var orderBy = query.Substring(orderByIndex).Replace(";", string.Empty);
			query = query.Substring(0, orderByIndex);

			var fromIndex = query.IndexOf("FROM ");
			var from = query.Substring(fromIndex);
			var select = query.Substring(0, fromIndex);

			var value = MsSqlStatements.PagedQueryFormat.FormatWith(select, orderBy, from);
			return value;
		}
		private static string RemovePaging(string query)
		{
			return query
				.Replace("\n LIMIT @Limit OFFSET @Skip;", ";")
				.Replace("\n LIMIT @Limit;", ";");
		}

		public override bool IsDuplicate(Exception exception)
		{
			var dbException = exception as SqlException;
			return dbException != null && dbException.Number == UniqueKeyViolation;
		}
	}
}