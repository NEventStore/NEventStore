namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	public abstract class CommonSqlDialect : ISqlDialect
	{
		public abstract string InitializeStorage { get; }

		public virtual string GetCommitsFromStartingRevision
		{
			get { return CommonSqlStatements.GetCommitsFromStartingRevision; }
		}
		public virtual string GetCommitsFromInstant
		{
			get { return CommonSqlStatements.GetCommitsFromInstant; }
		}
		public virtual string PersistCommit
		{
			get { return CommonSqlStatements.PersistCommit; }
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

		public virtual string StreamId
		{
			get { return "@StreamId"; }
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

		public virtual bool IsDuplicate(Exception exception)
		{
			var message = exception.Message.ToUpperInvariant();
			return message.Contains("DUPLICATE") || message.Contains("UNIQUE") || message.Contains("CONSTRAINT");
		}

		public virtual IDbTransaction OpenTransaction(IDbConnection connection)
		{
			return null;
		}
		public virtual IDbStatement BuildStatement(
			IDbConnection connection,
			IDbTransaction transaction,
			params IDisposable[] resources)
		{
			return new CommonDbStatement(this, connection, transaction, resources);
		}
	}
}