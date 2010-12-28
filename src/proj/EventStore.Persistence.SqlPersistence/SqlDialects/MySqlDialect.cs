namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;

	public class MySqlDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { yield return MySqlStatements.InitializeStorage; }
		}
		public override IEnumerable<string> PersistCommitAttempt
		{
			get
			{
				yield return CommonSqlStatements.PersistCommitAttempt
					.Replace("/*", string.Empty)
					.Replace("*/", string.Empty);
			}
		}
	}
}