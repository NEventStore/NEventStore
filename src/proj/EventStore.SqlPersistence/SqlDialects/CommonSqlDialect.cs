namespace EventStore.SqlPersistence.SqlDialects
{
	using System.Data.Common;

	public class CommonSqlDialect : ISqlDialect
	{
		public string AppendSnapshotToCommit
		{
			get { return CommonSqlStatements.AppendSnapshotToCommit; }
		}
		public string GetCommitsFromSnapshotUntilRevision
		{
			get { return CommonSqlStatements.GetCommitsFromSnapshotUntilRevision; }
		}
		public string GetCommitsFromStartingRevision
		{
			get { return CommonSqlStatements.GetCommitsFromStartingRevision; }
		}
		public string GetStreamsRequiringSnaphots
		{
			get { return CommonSqlStatements.GetStreamsRequiringSnaphots; }
		}
		public string GetUndispatchedCommits
		{
			get { return CommonSqlStatements.GetUndispatchedCommits; }
		}
		public string MarkCommitAsDispatched
		{
			get { return CommonSqlStatements.MarkCommitAsDispatched; }
		}
		public string PersistCommitAttempt
		{
			get { return CommonSqlStatements.PersistCommitAttempt; }
		}

		public string StreamId
		{
			get { return "@StreamId"; }
		}
		public string StreamName
		{
			get { return "@StreamName"; }
		}
		public string CommitId
		{
			get { return "@CommitId"; }
		}
		public string CommitSequence
		{
			get { return "@Sequence"; }
		}
		public string ExpectedRevision
		{
			get { return "@ExpectedRevision"; }
		}
		public string Revision
		{
			get { return "@Revision"; }
		}
		public string Payload
		{
			get { return "@Payload"; }
		}
		public string Threshold
		{
			get { return "@Threshold"; }
		}

		public bool IsDuplicateException(DbException exception)
		{
			var msg = exception.Message.ToUpperInvariant();
			return msg.Contains("DUPLICATE") || msg.Contains("UNIQUE");
		}
	}
}