namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;
	using System.Data.SqlClient;

	public class MsSqlDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;
		private const int ConstraintViolation = 515;

		public override bool IsDuplicateKey(DbException exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException == null)
				return false;

			return sqlException.Number == PrimaryKeyViolation || sqlException.Number == UniqueIndexViolation;
		}

		public override bool IsConstraintViolation(DbException exception)
		{
			var sqlException = exception as SqlException;
			return (null != sqlException && sqlException.Number == ConstraintViolation)
				|| base.IsConstraintViolation(exception);
		}

		public virtual string GetSelectEventsQuery
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public virtual string GetSelectEventsForCommandQuery
		{
			get { return MsSqlStatements.SelectEventsForCommand; }
		}
		public virtual string GetSelectEventsSinceVersionQuery
		{
			get { return MsSqlStatements.SelectEventsSinceVersion; }
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