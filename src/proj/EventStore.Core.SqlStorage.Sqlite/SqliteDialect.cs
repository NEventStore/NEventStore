namespace EventStore.Core.SqlStorage.Sqlite
{
	using System.Data;
	using System.Data.Common;

	public sealed class SqliteDialect : SqlDialect
	{
		private const string ConstraintViolation = "constraint";
		private const string UniqueViolation = "unique";

		public SqliteDialect(IDbConnection connection, IDbTransaction transaction)
			: base(connection, transaction)
		{
		}

		public override string SelectEvents
		{
			get { return SqliteStatements.SelectEvents; }
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
			return message.IndexOf(ConstraintViolation) > 0
			       && message.IndexOf(UniqueViolation) > 0;
		}
	}
}