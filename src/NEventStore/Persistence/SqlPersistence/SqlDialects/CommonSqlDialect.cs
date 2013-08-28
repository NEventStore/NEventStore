namespace NEventStore.Persistence.SqlPersistence.SqlDialects
{
    using System;
    using System.Data;
    using System.Transactions;

    public abstract class CommonSqlDialect : ISqlDialect
    {
        public abstract string InitializeStorage { get; }

        public virtual string PurgeStorage
        {
            get { return CommonSqlStatements.PurgeStorage; }
        }

        public string PurgeBucket
        {
            get { return CommonSqlStatements.PurgeBucket; }
        }

        public virtual string Drop
        {
            get { return CommonSqlStatements.DropTables; }
        }

        public virtual string GetCommitsFromStartingRevision
        {
            get { return CommonSqlStatements.GetCommitsFromStartingRevision; }
        }

        public virtual string GetCommitsFromInstant
        {
            get { return CommonSqlStatements.GetCommitsFromInstant; }
        }

        public virtual string GetCommitsFromToInstant
        {
            get { return CommonSqlStatements.GetCommitsFromToInstant; }
        }

        public virtual string PersistCommit
        {
            get { return CommonSqlStatements.PersistCommit; }
        }

        public virtual string DuplicateCommit
        {
            get { return CommonSqlStatements.DuplicateCommit; }
        }

        public virtual string GetStreamsRequiringSnapshots
        {
            get { return CommonSqlStatements.GetStreamsRequiringSnapshots; }
        }

        public virtual string GetSnapshot
        {
            get { return CommonSqlStatements.GetSnapshot; }
        }

        public virtual string AppendSnapshotToCommit
        {
            get { return CommonSqlStatements.AppendSnapshotToCommit; }
        }

        public virtual string GetUndispatchedCommits
        {
            get { return CommonSqlStatements.GetUndispatchedCommits; }
        }

        public virtual string MarkCommitAsDispatched
        {
            get { return CommonSqlStatements.MarkCommitAsDispatched; }
        }

        public virtual string BucketId
        {
            get { return "@BucketId"; }
        }

        public virtual string StreamId
        {
            get { return "@StreamId"; }
        }

        public virtual string StreamIdOriginal
        {
            get { return "@StreamIdOriginal"; }
        }

        public virtual string StreamRevision
        {
            get { return "@StreamRevision"; }
        }

        public virtual string MaxStreamRevision
        {
            get { return "@MaxStreamRevision"; }
        }

        public virtual string Items
        {
            get { return "@Items"; }
        }

        public virtual string CommitId
        {
            get { return "@CommitId"; }
        }

        public virtual string CommitSequence
        {
            get { return "@CommitSequence"; }
        }

        public virtual string CommitStamp
        {
            get { return "@CommitStamp"; }
        }

        public virtual string CommitStampStart
        {
            get { return "@CommitStampStart"; }
        }

        public virtual string CommitStampEnd
        {
            get { return "@CommitStampEnd"; }
        }

        public virtual string Headers
        {
            get { return "@Headers"; }
        }

        public virtual string Payload
        {
            get { return "@Payload"; }
        }

        public virtual string Threshold
        {
            get { return "@Threshold"; }
        }

        public virtual string Limit
        {
            get { return "@Limit"; }
        }

        public virtual string Skip
        {
            get { return "@Skip"; }
        }

        public virtual bool CanPage
        {
            get { return true; }
        }

        public string CheckpointNumber
        {
            get { return "@CheckpointNumber"; }
        }

        public virtual string GetCommitsSinceCheckpoint
        {
            get { return CommonSqlStatements.GetCommitsSinceCheckpoint; }
        }

        public virtual object CoalesceParameterValue(object value)
        {
            return value;
        }

        public virtual bool IsDuplicate(Exception exception)
        {
            string message = exception.Message.ToUpperInvariant();
            return message.Contains("DUPLICATE") || message.Contains("UNIQUE") || message.Contains("CONSTRAINT");
        }

        public virtual IDbTransaction OpenTransaction(IDbConnection connection)
        {
            return null;
        }

        public virtual IDbStatement BuildStatement(
            TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
        {
            return new CommonDbStatement(this, scope, connection, transaction);
        }
    }
}