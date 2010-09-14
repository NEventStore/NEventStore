namespace EventStore.Core.SqlStorage.Sqlite
{
	using System;
	using System.Data;
	using System.Data.Common;

	public sealed class SqliteDialect : BaseDialect
	{
		private const string ConstraintViolation = "constraint";
		private const string UniqueViolation = "unique";

		public SqliteDialect(IDbConnection connection, IDbTransaction transaction, Guid tenantId)
			: base(connection, transaction, tenantId)
		{
		}

		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
		}
		public override string SelectEventsForCommand
		{
			get { return SqliteStatements.SelectEventsForCommand; }
		}
		public override string SelectEventsForVersion
		{
			get { return SqliteStatements.SelectEventsForVersion; }
		}
		public override string InsertEvent
		{
			get { return SqliteStatements.InsertEvent; }
		}
		public override string InsertEvents
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