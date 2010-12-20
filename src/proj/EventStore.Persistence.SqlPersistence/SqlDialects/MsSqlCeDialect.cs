namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class MsSqlCeDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MsSqlCeStatements.InitializeStorage; }
		}
	}
}