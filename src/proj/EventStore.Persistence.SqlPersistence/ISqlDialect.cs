namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	public interface ISqlDialect
	{
		IEnumerable<string> InitializeStorage { get; }
		IEnumerable<string> AppendSnapshotToCommit { get; }
		string GetCommitsFromSnapshotUntilRevision { get; }
		string GetCommitsFromStartingRevision { get; }
		string GetStreamsRequiringSnaphots { get; }
		string GetUndispatchedCommits { get; }
		string MarkCommitAsDispatched { get; }
		IEnumerable<string> PersistCommitAttempt { get; }

		string StreamId { get; }
		string StreamName { get; }
		string CommitId { get; }
		string CommitSequence { get; }
		string StreamRevision { get; }
		string Headers { get; }
		string Payload { get; }
		string Threshold { get; }

		void AmmendStatement(IDbCommand command);
		bool IsDuplicateException(Exception exception);
	}
}