namespace NEventStore.Persistence.SqlPersistence.SqlDialects
{
	public class SqliteDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqliteStatements.InitializeStorage; }
		}

		// Sqlite wants all parameters to be a part of the query
		public override string GetCommitsFromStartingRevision
		{
			get { return base.GetCommitsFromStartingRevision.Replace("\n ORDER BY ", "\n  AND @Skip = @Skip\nORDER BY "); }
		}
	}
}