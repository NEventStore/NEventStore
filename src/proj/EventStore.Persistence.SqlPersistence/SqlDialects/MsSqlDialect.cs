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
			get { return Paged(base.GetCommitsFromInstant); }
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