namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;

	public class MySqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return MySqlStatements.InitializeStorage; }
		}
		public override string PersistCommitAttempt
		{
			get { return CommonSqlStatements.PersistCommitAttempt.Replace("/*FROM DUAL*/", "FROM DUAL"); }
		}

		public override IDbStatement BuildStatement(IDbConnection connection)
		{
			return new MySqlDbStatement(connection);
		}

		private class MySqlDbStatement : CommonDbStatement
		{
			public MySqlDbStatement(IDbConnection connection)
				: base(connection)
			{
			}

			public override void AddParameter(string name, object value)
			{
				if (value is Guid)
					value = ((Guid)value).ToByteArray();

				base.AddParameter(name, value);
			}
		}
	}
}