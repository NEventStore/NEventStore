namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;

	public class SqliteDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
	{
		private const string ConstraintViolation = "constraint";
		private const string UniqueViolation = "unique";

		public override bool IsDuplicateKey(DbException exception)
		{
			var message = exception.Message.ToLowerInvariant();
			return message.Contains(ConstraintViolation) && message.Contains(UniqueViolation);
		}

		public virtual string GetSelectEventsQuery
		{
			get { return SqliteStatements.SelectEvents; }
		}
		public virtual string GetSelectEventsForCommandQuery
		{
			get { return SqliteStatements.SelectEventsForCommand; }
		}
		public virtual string GetSelectEventsForVersionQuery
		{
			get { return SqliteStatements.SelectEventsForVersion; }
		}
		public virtual string GetInsertEventsCommand
		{
			get { return SqliteStatements.InsertEvents; }
		}
		public virtual string GetInsertEventCommand
		{
			get { return SqliteStatements.InsertEvent; }
		}
	}
}