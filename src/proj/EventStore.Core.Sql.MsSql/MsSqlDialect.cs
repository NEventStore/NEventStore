namespace EventStore.Core.Sql.MsSql
{
	using System.Data.Common;

	public sealed class MsSqlDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public override string SelectEventsWhere
		{
			get { return MsSqlStatements.SelectEventsWhere; }
		}
		public override string InsertEvents
		{
			get { return MsSqlStatements.InsertEvents; }
		}
		public override string InsertEvent
		{
			get { return MsSqlStatements.InsertEvent; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			return exception.ErrorCode == PrimaryKeyViolation || exception.ErrorCode == UniqueIndexViolation;
		}
	}
}