namespace EventStore.Core.Sql.MsSql
{
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	public sealed class MsSqlDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public MsSqlDialect(IDbConnection connection)
			: base(connection)
		{
		}

		public override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
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