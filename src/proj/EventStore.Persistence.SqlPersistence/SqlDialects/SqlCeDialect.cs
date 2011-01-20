namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;

	public class SqlCeDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqlCeStatements.InitializeStorage; }
		}

		public override IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources)
		{
			return new SqlCeDbStatement(connection, transaction, resources);
		}

		private class SqlCeDbStatement : DelimitedDbStatement
		{
			public SqlCeDbStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources)
				: base(connection, transaction, resources)
			{
			}
		}
	}
}