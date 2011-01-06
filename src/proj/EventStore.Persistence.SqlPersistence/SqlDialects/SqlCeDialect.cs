namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public class SqlCeDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqlCeStatements.InitializeStorage; }
		}

		public override IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction)
		{
			return new SqlCeDbStatement(connection, transaction);
		}

		private class SqlCeDbStatement : DelimitedDbStatement
		{
			public SqlCeDbStatement(IDbConnection connection, IDbTransaction transaction)
				: base(connection, transaction)
			{
			}
		}
	}
}