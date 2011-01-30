namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	public class MsSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT TOP 1 *").Replace("LIMIT 1", string.Empty); }
		}
	}
}