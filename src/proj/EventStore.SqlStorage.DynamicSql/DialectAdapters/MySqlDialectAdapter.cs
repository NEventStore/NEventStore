namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;

	public class MySqlDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
	{
		private const string DuplicateEntryText = "Duplicate entry";
		private const string KeyViolationText = "for key";

		public override bool IsDuplicateKey(DbException exception)
		{
			if (exception == null)
				return false;

			var message = exception.Message;
			return message.Contains(DuplicateEntryText) && message.Contains(KeyViolationText);
		}

		public virtual string GetSelectEventsQuery
		{
			get { return MySqlStatements.SelectEvents; }
		}
		public virtual string GetSelectEventsForCommandQuery
		{
			get { return MySqlStatements.SelectEventsForCommand; }
		}
		public virtual string GetSelectEventsForVersionQuery
		{
			get { return MySqlStatements.SelectEventsForVersion; }
		}
		public virtual string GetInsertEventsCommand
		{
			get { return MySqlStatements.InsertEvents; }
		}
		public virtual string GetInsertEventCommand
		{
			get { return MySqlStatements.InsertEvent; }
		}
	}
}