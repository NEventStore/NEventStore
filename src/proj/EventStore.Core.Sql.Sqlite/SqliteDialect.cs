namespace EventStore.Core.Sql.Sqlite
{
	using System;
	using System.Data;
	using System.Data.Common;

	public sealed class SqliteDialect : SqlDialect
	{
		public SqliteDialect(IDbConnection connection)
			: base(connection)
		{
		}

		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			return exception.Message.ToLowerInvariant().Contains("unique");
		}
	}
}