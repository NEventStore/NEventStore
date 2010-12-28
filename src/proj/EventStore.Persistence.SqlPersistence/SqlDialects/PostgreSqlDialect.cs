namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class PostgreSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return PostgreSqlStatements.InitializeStorage; }
		}

		public override string PersistCommitAttempt
		{
			get { return base.PersistCommitAttempt.Replace(this.Delimiter, string.Empty); }
		}
	}
}