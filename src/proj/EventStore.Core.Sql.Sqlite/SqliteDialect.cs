namespace EventStore.Core.Sql.Sqlite
{
	using System;
	using System.Data.Common;

	public sealed class SqliteDialect : SqlDialect
	{
		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			throw new NotImplementedException();
		}
	}
}