namespace EventStore.SqlPersistence
{
	using System.Data.Common;

	public interface ISqlDialect
	{
		string AppendSnapshotToCommit { get; }
		string GetCommitsFromSnapshotUntilRevision { get; }
		string GetCommitsFromStartingRevision { get; }
		string GetStreamsRequiringSnaphots { get; }
		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }
		string PersistCommitAttempt { get; }

		string StreamId { get; }
		string StreamName { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string ExpectedRevision { get; }
		string Revision { get; }
		string Payload { get; }
		string Threshold { get; }

		bool IsDuplicateException(DbException exception);
	}
}