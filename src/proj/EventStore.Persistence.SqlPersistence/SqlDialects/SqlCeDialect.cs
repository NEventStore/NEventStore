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

		public override IDbStatement BuildStatement(IDbConnection connection)
		{
			return new SqlCeDbStatement(connection);
		}

		private class SqlCeDbStatement : DelimitedDbStatement
		{
			public SqlCeDbStatement(IDbConnection connection)
				: base(connection)
			{
			}

			protected override void SetParameterValue(IDataParameter param, object value, DbType? type)
			{
				if (value is Guid)
					base.SetParameterValue(param, value, DbType.Guid);
				else if (value is long)
					base.SetParameterValue(param, value, DbType.Int64);
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