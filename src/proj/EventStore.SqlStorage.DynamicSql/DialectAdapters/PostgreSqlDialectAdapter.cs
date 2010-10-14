namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;

	public class PostgreSqlDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
	{
		private const string DuplicateEntryText = "duplicate key";

		public override bool IsDuplicateKey(DbException exception)
		{
			return exception != null
				&& !string.IsNullOrEmpty(exception.Message)
				&& exception.Message.ToLowerInvariant().Contains(DuplicateEntryText);
		}

		public virtual string GetSelectEventsQuery
		{
			get { return PostgreSqlStatements.SelectEvents; }
		}
		public virtual string GetSelectEventsForCommandQuery
		{
			get { return PostgreSqlStatements.SelectEventsForCommand; }
		}
		public virtual string GetSelectEventsSinceVersionQuery
		{
			get { return PostgreSqlStatements.SelectEventsSinceVersion; }
		}
		public virtual string GetInsertEventsCommand
		{
			get { return PostgreSqlStatements.InsertEvents; }
		}
		public virtual string GetInsertEventCommand
		{
			get { return PostgreSqlStatements.InsertEvent; }
		}
	}
}