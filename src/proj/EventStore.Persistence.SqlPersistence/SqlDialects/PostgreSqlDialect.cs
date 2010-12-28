namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;

	public class PostgreSqlDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { yield return PostgreSqlStatements.InitializeStorage; }
		}
	}
}