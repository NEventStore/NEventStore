namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public class MySqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MySqlStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get
			{
				return CommonSqlStatements.PersistCommitAttempt
					.Replace("/*", string.Empty)
					.Replace("*/", string.Empty);
			}
		}

		public override DbType GuidType
		{
			get { return DbType.Binary; }
		}
	}
}