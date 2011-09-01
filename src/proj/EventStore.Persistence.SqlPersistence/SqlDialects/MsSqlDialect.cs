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
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT TOP 1 *").Replace("LIMIT 1", string.Empty); }
		}

		public override string GetStreamsRequiringSnapshots
		{
			get { return Paged(base.GetStreamsRequiringSnapshots); }
		}
		public override string GetCommitsFromInstant
		{
			get { return Paged(base.GetCommitsFromInstant); }
		}
		public override string GetCommitsFromStartingRevision
		{
			get { return Paged(base.GetCommitsFromStartingRevision); }
		}
		public override string GetUndispatchedCommits
		{
			get { return Paged(base.GetUndispatchedCommits); }
		}
		private static string Paged(string statement)
		{
			var orderByIndex = statement.IndexOf("ORDER BY");
			var orderBy = statement.Substring(orderByIndex).Replace(";", string.Empty);
			statement = statement.Substring(0, orderByIndex);

			var fromIndex = statement.IndexOf("FROM ");
			var from = statement.Substring(fromIndex);
			var select = statement.Substring(0, fromIndex);

			return MsSqlStatements.PagedQueryFormat.FormatWith(select, orderBy, from);
		}
		public override bool CanPage
		{
			get { return true; }
		}

		public override bool IsDuplicate(Exception exception)
		{
			var dbException = exception as SqlException;
			return dbException != null && dbException.Number == UniqueKeyViolation;
		}
	}
}