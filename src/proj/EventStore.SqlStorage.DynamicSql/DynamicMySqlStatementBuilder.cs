namespace EventStore.SqlStorage.DynamicSql
{
	using System.Data.Common;
	using DynamicSql;

	public sealed class DynamicMySqlStatementBuilder : DynamicSqlStatementBuilder
	{
		private const string DuplicateEntryText = "Duplicate entry";
		private const string KeyViolationText = "for key";

		public DynamicMySqlStatementBuilder(CommandBuilder builder)
			: base(builder)
		{
		}

		protected override string SelectEvents
		{
			get { return MySqlStatements.SelectEvents; }
		}
		protected override string SelectEventsForCommand
		{
			get { return MySqlStatements.SelectEventsForCommand; }
		}
		protected override string SelectEventsForVersion
		{
			get { return MySqlStatements.SelectEventsForVersion; }
		}
		protected override string InsertEvent
		{
			get { return MySqlStatements.InsertEvent; }
		}
		protected override string InsertEvents
		{
			get { return MySqlStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			if (exception == null)
				return false;

			var message = exception.Message;
			return message.Contains(DuplicateEntryText) && message.Contains(KeyViolationText);
		}
	}
}