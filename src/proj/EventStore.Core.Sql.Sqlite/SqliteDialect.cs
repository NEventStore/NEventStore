namespace EventStore.Core.Sql.Sqlite
{
	using System.Data;
	using System.Data.Common;

	public sealed class SqliteDialect : SqlDialect
	{
		public SqliteDialect(IDbConnection connection, IDbTransaction transaction)
			: base(connection, transaction)
		{
		}

		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}
		public override string InsertEvent
		{
			get { return SqliteStatements.InsertEvent; }
		}
		public override string InsertEvents
		{
			get { return SqliteStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			return exception.Message.ToLowerInvariant().Contains("unique");
		}
	}
}