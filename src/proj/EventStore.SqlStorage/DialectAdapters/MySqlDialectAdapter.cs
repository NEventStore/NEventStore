namespace EventStore.SqlStorage.DialectAdapters
{
	using System.Data.Common;

	public class MySqlDialectAdapter : CommonSqlDialectAdapter
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

		public override string GetSelectEventsQuery
		{
			get { return MySqlStatements.SelectEvents; }
		}
		public override string GetSelectEventsForCommandQuery
		{
			get { return MySqlStatements.SelectEventsForCommand; }
		}
		public override string GetSelectEventsForVersionQuery
		{
			get { return MySqlStatements.SelectEventsForVersion; }
		}
		public override string GetInsertEventsCommand
		{
			get { return MySqlStatements.InsertEvents; }
		}
		public override string GetInsertEventCommand
		{
			get { return MySqlStatements.InsertEvent; }
		}
	}
}