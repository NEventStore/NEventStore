namespace EventStore.Core.Sql.Sqlite
{
	using System.Data.Common;

	public sealed class SqliteDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}
		public override string SelectEventsWhere
		{
			get { return SqliteStatements.SelectEventsWhere; }
		}
		public override string InsertEvents
		{
			get { return SqliteStatements.InsertEvents; }
		}
		public override string InsertEvent
		{
			get { return SqliteStatements.InsertEvent; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			return exception.ErrorCode == PrimaryKeyViolation || exception.ErrorCode == UniqueIndexViolation;
		}
	}
}