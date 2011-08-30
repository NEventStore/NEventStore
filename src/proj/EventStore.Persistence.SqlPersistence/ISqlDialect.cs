namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface ISqlDialect
	{
		string InitializeStorage { get; }

		string GetCommitsFromStartingRevision { get; }
		string GetCommitsFromInstant { get; }

		string PersistCommit { get; }

		string GetStreamsRequiringSnapshots { get; }
		string GetSnapshot { get; }
		string AppendSnapshotToCommit { get; }

		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }

		string StreamId { get; }
		string StreamRevision { get; }
		string MaxStreamRevision { get; }
		string Items { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string CommitStamp { get; }
		string Headers { get; }
		string Payload { get; }
		string Threshold { get; }

		IDbTransaction OpenTransaction(IDbConnection connection);
		IDbStatement BuildStatement(IDbConnection connection, IDbTransaction transaction, params IDisposable[] resources);

		bool IsDuplicate(Exception exception);
	}
}