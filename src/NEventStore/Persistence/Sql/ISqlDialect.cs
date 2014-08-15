namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;
    using System.Transactions;
    using NEventStore.Persistence.Sql.SqlDialects;

    public interface ISqlDialect
    {
        string InitializeStorage { get; }
        string PurgeStorage { get; }
        string PurgeBucket { get; }
        string Drop { get; }
        string DeleteStream { get; }

        string GetCommitsFromStartingRevision { get; }
        string GetCommitsFromInstant { get; }
        string GetCommitsFromToInstant { get; }

        string PersistCommit { get; }
        string DuplicateCommit { get; }

        string GetStreamsRequiringSnapshots { get; }
        string GetSnapshot { get; }
        string AppendSnapshotToCommit { get; }

        string BucketId { get; }
        string StreamId { get; }
        string StreamIdOriginal { get; }
        string StreamRevision { get; }
        string MaxStreamRevision { get; }
        string Items { get; }
        string CommitId { get; }
        string CommitSequence { get; }
        string CommitStamp { get; }
        string CommitStampStart { get; }
        string CommitStampEnd { get; }
        string Headers { get; }
        string Payload { get; }
        string Threshold { get; }

        string Limit { get; }
        string Skip { get; }
        bool CanPage { get; }
        string CheckpointNumber { get; }
        string GetCommitsFromCheckpoint { get; }

        object CoalesceParameterValue(object value);

        IDbTransaction OpenTransaction(IDbConnection connection);

        IDbStatement BuildStatement(
            TransactionScope scope, IDbConnection connection, IDbTransaction transaction);

        bool IsDuplicate(Exception exception);

        void AddPayloadParamater(IConnectionFactory connectionFactory, IDbConnection connection, IDbStatement cmd, byte[] payload);

        DateTime ToDateTime(object value);

        NextPageDelegate NextPageDelegate { get; }
    }
}