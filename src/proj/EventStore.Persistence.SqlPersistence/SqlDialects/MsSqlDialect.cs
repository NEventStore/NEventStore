namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Collections.Generic;

	public class MsSqlDialect : CommonSqlDialect
	{
		public override IEnumerable<string> InitializeStorage
		{
			get { yield return MsSqlStatements.InitializeStorage; }
		}
	}
}