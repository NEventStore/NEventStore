namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface ISqlDialect
	{
		string InitializeStorage { get; }
		string AppendSnapshotToCommit { get; }
		string GetCommitsFromSnapshotUntilRevision { get; }
		string GetCommitsFromStartingRevision { get; }
		string GetCommitsFromInstant { get; }
		string GetStreamsRequiringSnaphots { get; }
		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }
		string PersistCommitAttempt { get; }

		string StreamId { get; }
		string StreamRevision { get; }
		string Items { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string CommitStamp { get; }
		string Headers { get; }
		string Payload { get; }
		string Threshold { get; }

		IDbTransaction OpenTransaction(IDbConnection connection);
		IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources);
	}
}