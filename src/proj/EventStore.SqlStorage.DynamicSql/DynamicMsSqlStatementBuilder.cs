namespace EventStore.SqlStorage.DynamicSql
{
	using System.Data.Common;
	using System.Data.SqlClient;

	public sealed class DynamicMsSqlStatementBuilder : DynamicSqlStatementBuilder
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public DynamicMsSqlStatementBuilder(CommandBuilder builder)
			: base(builder)
		{
		}

		protected override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		protected override string SelectEventsForCommand
		{
			get { return MsSqlStatements.SelectEventsForCommand; }
		}
		protected override string SelectEventsForVersion
		{
			get { return MsSqlStatements.SelectEventsForVersion; }
		}
		protected override string InsertEvent
		{
			get { return MsSqlStatements.InsertEvent; }
		}
		protected override string InsertEvents
		{
			get { return MsSqlStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException == null)
				return false;

			return sqlException.Number == PrimaryKeyViolation || sqlException.Number == UniqueIndexViolation;
		}
	}
}