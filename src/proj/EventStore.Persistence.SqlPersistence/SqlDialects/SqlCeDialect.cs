namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;

	public class SqlCeDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return SqlCeStatements.InitializeStorage; }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("SELECT *", "SELECT TOP(1) *").Replace("LIMIT 1", string.Empty); }
		}
		public override string GetStreamsRequiringSnapshots
		{
			get { return base.GetStreamsRequiringSnapshots.NonPaged(); }
		}
		public override string GetCommitsFromInstant
		{
			get { return base.GetCommitsFromInstant.NonPaged(); }
		}
		public override string GetCommitsFromStartingRevision
		{
			get { return base.GetCommitsFromStartingRevision.NonPaged(); }
		}
		public override string GetUndispatchedCommits
		{
			get { return base.GetUndispatchedCommits.NonPaged(); }
		}
		public override bool CanPage
		{
			get { return false; }
		}

		public override bool IsDuplicate(Exception exception)
		{
			// TODO: better way without using reflection and avoiding a reference to SqlCE?
			var message = exception.Message.ToUpperInvariant();
			return message.Contains("DUPLICATE") || message.Contains("UNIQUE");
		}

		public override IDbStatement BuildStatement(
			IDbConnection connection,
			IDbTransaction transaction,
			params IDisposable[] resources)
		{
			return new SqlCeDbStatement(this, connection, transaction, resources);
		}

		private class SqlCeDbStatement : DelimitedDbStatement
		{
			public SqlCeDbStatement(
				ISqlDialect dialect,
				IDbConnection connection,
				IDbTransaction transaction,
				params IDisposable[] resources)
				: base(dialect, connection, transaction, resources)
			{
			}
		}
	}
}