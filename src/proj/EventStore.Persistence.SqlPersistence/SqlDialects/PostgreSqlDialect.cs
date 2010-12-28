namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

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

		public override DbType GuidType
		{
			get { return DbType.Binary; }
		}
	}
}