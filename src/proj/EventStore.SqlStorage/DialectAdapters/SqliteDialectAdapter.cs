namespace EventStore.SqlStorage.DialectAdapters
{
	using System.Data.Common;

	public class SqliteDialectAdapter : CommonSqlDialectAdapter
	{
		private const string ConstraintViolation = "constraint";
		private const string UniqueViolation = "unique";

		public override bool IsDuplicateKey(DbException exception)
		{
			var message = exception.Message.ToLowerInvariant();
			return message.Contains(ConstraintViolation) && message.Contains(UniqueViolation);
		}

		public override string GetSelectEventsQuery
		{
			get { return SqliteStatements.SelectEvents; }
		}
		public override string GetSelectEventsForCommandQuery
		{
			get { return SqliteStatements.SelectEventsForCommand; }
		}
		public override string GetSelectEventsForVersionQuery
		{
			get { return SqliteStatements.SelectEventsForVersion; }
		}
		public override string GetInsertEventsCommand
		{
			get { return SqliteStatements.InsertEvents; }
		}
		public override string GetInsertEventCommand
		{
			get { return SqliteStatements.InsertEvent; }
		}
	}
}