namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data.SqlClient;

	public class MsSqlDialect : CommonSqlDialect
	{
		private const int UniqueKeyViolation = 2627;

		public override string InitializeStorage
		{
			get { return MsSqlStatements.InitializeStorage; }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT TOP 1 *").Replace("LIMIT 1", string.Empty); }
		}

        public override bool IsDuplicate(Exception exception)
		{
            return exception.Message.Contains("IX_Commits");
		}

        public override bool IsConcurrencyException(Exception exception)
        {
            var dbException = exception as SqlException;
            return dbException != null && dbException.Number == UniqueKeyViolation;
        }
    }
}