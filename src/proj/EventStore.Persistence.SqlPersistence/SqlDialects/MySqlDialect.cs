namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;

	public class MySqlDialect : CommonSqlDialect
	{
		private const int UniqueKeyViolation = 1062;

		public override string InitializeStorage
		{
			get { return MySqlStatements.InitializeStorage; }
		}
		public override string PersistCommit
		{
			get { return CommonSqlStatements.PersistCommit.Replace("/*FROM DUAL*/", "FROM DUAL"); }
		}
		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace("/*FROM DUAL*/", "FROM DUAL"); }
		}
		public override string MarkCommitAsDispatched
		{
			get { return base.MarkCommitAsDispatched.Replace("1", "true"); }
		}

		public override string GetStreamsRequiringSnapshots
		{
			get { return Paged(base.GetStreamsRequiringSnapshots); }
		}
		public override string GetCommitsFromInstant
		{
			get { return Paged(base.GetCommitsFromInstant); }
		}
		public override string GetCommitsFromStartingRevision
		{
			get { return Paged(base.GetCommitsFromStartingRevision); }
		}
		public override string GetUndispatchedCommits
		{
			get { return Paged(base.GetUndispatchedCommits.Replace("0", "false")); }
		}
		private static string Paged(string statement)
		{
			return statement.Replace(";", "\nLIMIT @Skip, @Limit;");
		}

		public override bool IsDuplicate(Exception exception)
		{
			var property = exception.GetType().GetProperty("Number");
			return UniqueKeyViolation == (int)property.GetValue(exception, null);
		}

		public override IDbStatement BuildStatement(
			IDbConnection connection,
			IDbTransaction transaction,
			params IDisposable[] resources)
		{
			return new MySqlDbStatement(this, connection, transaction, resources);
		}

		private class MySqlDbStatement : CommonDbStatement
		{
			public MySqlDbStatement(
				ISqlDialect dialect,
				IDbConnection connection,
				IDbTransaction transaction,
				params IDisposable[] resources)
				: base(dialect, connection, transaction, resources)
			{
			}

			public override void AddParameter(string name, object value)
			{
				if (value is Guid)
					value = ((Guid)value).ToByteArray();

				if (value is DateTime)
					value = ((DateTime)value).Ticks;

				base.AddParameter(name, value);
			}
		}
	}
}