namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class SqlCeDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqlCeStatements.InitializeStorage; }
		}
	}
}