namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;

	public class PostgreSqlDialectAdapter : CommonSqlDialectAdapter, IAdaptDynamicSqlDialect
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

		public override string NormalizeParameterName(string parameterName)
		{
			return ":" + parameterName;
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