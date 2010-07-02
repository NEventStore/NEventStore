namespace EventStore.Core.SqlStorage.MsSql
{
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	public sealed class MsSqlDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public MsSqlDialect(IDbConnection connection, IDbTransaction transaction)
			: base(connection, transaction)
		{
		}

		public override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public override string InsertEvent
		{
			get { return MsSqlStatements.InsertEvent; }
		}
		public override string InsertEvents
		{
			get { return MsSqlStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException == null)
				return false;

			return sqlException.Number == PrimaryKeyViolation || sqlException.Number == UniqueIndexViolation;
		}
	}
}