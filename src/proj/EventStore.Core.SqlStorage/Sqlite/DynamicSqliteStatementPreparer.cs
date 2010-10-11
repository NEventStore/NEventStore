namespace EventStore.Core.SqlStorage.Sqlite
{
	using System.Data.Common;

	public sealed class DynamicSqliteStatementPreparer : DynamicSqlStatementPreparer
	{
		private const string ConstraintViolation = "constraint";
		private const string UniqueViolation = "unique";

		public DynamicSqliteStatementPreparer(CommandBuilder builder)
			: base(builder)
		{
		}

		protected override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}
		protected override string SelectEventsForCommand
		{
			get { return SqliteStatements.SelectEventsForCommand; }
		}
		protected override string SelectEventsForVersion
		{
			get { return SqliteStatements.SelectEventsForVersion; }
		}
		protected override string InsertEvent
		{
			get { return SqliteStatements.InsertEvent; }
		}
		protected override string InsertEvents
		{
			get { return SqliteStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			var message = exception.Message.ToLowerInvariant();
			return message.Contains(ConstraintViolation) && message.Contains(UniqueViolation);
		}
	}
}