namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public class SqliteDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqliteStatements.InitializeStorage; }
		}

		public override IDbTransaction OpenTransaction(IDbConnection connection)
		{
			return connection.BeginTransaction(IsolationLevel.ReadCommitted);
		}
	}
}