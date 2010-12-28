namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;

	public class SqliteDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { yield return SqliteStatements.InitializeStorage; }
		}
	}
}