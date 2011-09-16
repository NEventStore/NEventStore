namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;
	using System.Transactions;

	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage; }
		}
		public override string PersistCommit
		{
			get { return base.PersistCommit.Replace("/*FROM DUAL*/", "FROM rdb$database"); }
		}
		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace("/*FROM DUAL*/", "FROM rdb$database"); }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT FIRST 1 *").Replace("LIMIT 1", string.Empty); }
		}

		public override string GetCommitsFromStartingRevision
		{
			get { return Paged(base.GetCommitsFromStartingRevision); }
		}
		public override string GetCommitsFromInstant
		{
			get { return Paged(base.GetCommitsFromInstant); }
		}
		public override string GetStreamsRequiringSnapshots
		{
			get { return Paged(base.GetStreamsRequiringSnapshots); }
		}
		private static string Paged(string query)
		{
			return query.Replace("SELECT ", "SELECT FIRST @Limit ").Replace("LIMIT @Limit;", ";");
		}

		public override IDbStatement BuildStatement(
			IDbConnection connection,
			IDbTransaction transaction,
			TransactionScope scope)
		{
			return new FirebirdDbStatement(this, connection, transaction, scope);
		}

		private class FirebirdDbStatement : DelimitedDbStatement
		{
			public FirebirdDbStatement(
				ISqlDialect dialect,
				IDbConnection connection,
				IDbTransaction transaction,
				TransactionScope scope)
				: base(dialect, connection, transaction, scope)
			{
			}
		}
	}
}