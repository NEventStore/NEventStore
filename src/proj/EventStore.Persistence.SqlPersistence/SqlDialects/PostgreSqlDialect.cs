namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class PostgreSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return PostgreSqlStatements.InitializeStorage; }
		}
		public override string GetUndispatchedCommits
		{
			get { return base.GetUndispatchedCommits.Replace("0", "false"); }
		}
		public override string MarkCommitAsDispatched
		{
			get { return base.MarkCommitAsDispatched.Replace("1", "true"); }
		}
	}
}