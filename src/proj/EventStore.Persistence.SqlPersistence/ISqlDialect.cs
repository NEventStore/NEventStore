namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;
	using System.Transactions;

	public interface ISqlDialect
	{
		string InitializeStorage { get; }
		string PurgeStorage { get; }

		string GetCommitsFromStartingRevision { get; }
		string GetCommitsFromInstant { get; }

		string PersistCommit { get; }
		string DuplicateCommit { get; }

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

		string Limit { get; }
		string Skip { get; }
		bool CanPage { get; }

		object CoalesceParameterValue(object value);

		IDbTransaction OpenTransaction(IDbConnection connection);
		IDbStatement BuildStatement(
			TransactionScope scope, IDbConnection connection, IDbTransaction transaction);

		bool IsDuplicate(Exception exception);
	}
}