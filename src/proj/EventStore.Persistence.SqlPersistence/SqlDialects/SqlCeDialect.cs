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

		public override IDbTransaction OpenTransaction(IDbConnection connection)
		{
			return connection.BeginTransaction(IsolationLevel.ReadCommitted);
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

			protected override void SetParameterValue(IDataParameter param, object value, DbType? type)
			{
				if (value is Guid)
					base.SetParameterValue(param, value, DbType.Guid);
				else if (value is int)
					base.SetParameterValue(param, value, DbType.Int32);
				else if (value is string)
					base.SetParameterValue(param, value, DbType.String);
				else if (value is byte[])
					base.SetParameterValue(param, value, DbType.Binary);
				else
					base.SetParameterValue(param, value, null);
			}
		}
	}
}