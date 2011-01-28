namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Data;

	public class FirebirdSqlDialect : CommonSqlDialect
	{
		public override string InitializeStorage
		{
			get { return FirebirdSqlStatements.InitializeStorage; }
		}
		public override string PersistCommit
		{
			get { return base.PersistCommit.Replace("/*FROM DUAL*/", "FROM rdb$database").Replace(", Snapshot", ", \"Snapshot\""); }
		}
		public override string GetStreamsRequiringSnaphots
		{
			get { return base.GetStreamsRequiringSnaphots.Replace("Snapshot ", "\"Snapshot\" "); }
		}
		public override string GetSnapshot
		{
			get { return base.GetSnapshot.Replace("Snapshot ", "\"Snapshot\" "); }
		}
		public override string AppendSnapshotToCommit
		{
			get { return base.AppendSnapshotToCommit.Replace("Snapshot ", "\"Snapshot\" "); }
		}

		public override IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources)
		{
			return new FirebirdDbStatement(connection, transaction, resources);
		}

		private class FirebirdDbStatement : DelimitedDbStatement
		{
			public FirebirdDbStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources)
				: base(connection, transaction, resources)
			{
			}
		}
	}
}