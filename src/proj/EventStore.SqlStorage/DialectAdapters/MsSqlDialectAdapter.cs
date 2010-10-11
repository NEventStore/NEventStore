namespace EventStore.SqlStorage.DialectAdapters
{
	using System.Data.Common;
	using System.Data.SqlClient;

	public class MsSqlDialectAdapter : CommonSqlDialectAdapter
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public override bool IsDuplicateKey(DbException exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException == null)
				return false;

			return sqlException.Number == PrimaryKeyViolation || sqlException.Number == UniqueIndexViolation;
		}

		public override string GetSelectEventsQuery
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public override string GetSelectEventsForCommandQuery
		{
			get { return MsSqlStatements.SelectEventsForCommand; }
		}
		public override string GetSelectEventsForVersionQuery
		{
			get { return MsSqlStatements.SelectEventsForVersion; }
		}
		public override string GetInsertEventsCommand
		{
			get { return MsSqlStatements.InsertEvents; }
		}
		public override string GetInsertEventCommand
		{
			get { return MsSqlStatements.InsertEvent; }
		}
	}
}