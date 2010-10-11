namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;
	using System.Data.SqlClient;

	public class MsSqlDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
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

		public virtual string GetSelectEventsQuery
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public virtual string GetSelectEventsForCommandQuery
		{
			get { return MsSqlStatements.SelectEventsForCommand; }
		}
		public virtual string GetSelectEventsForVersionQuery
		{
			get { return MsSqlStatements.SelectEventsForVersion; }
		}
		public virtual string GetInsertEventsCommand
		{
			get { return MsSqlStatements.InsertEvents; }
		}
		public virtual string GetInsertEventCommand
		{
			get { return MsSqlStatements.InsertEvent; }
		}
	}
}