namespace EventStore.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Transactions;
	using Persistence;
	using Serialization;

	public class SqlPersistence : IPersistStreams
	{
		private readonly IConnectionFactory factory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlPersistence(IConnectionFactory factory, ISqlDialect dialect, ISerialize serializer)
		{
			this.factory = factory;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return this.Fetch(streamId, maxRevision, this.dialect.GetCommitsFromSnapshotUntilRevision);
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return this.Fetch(streamId, minRevision, this.dialect.GetCommitsFromStartingRevision);
		}
		protected virtual IEnumerable<Commit> Fetch(Guid streamId, long revision, string queryText)
		{
			return this.Execute(streamId, query =>
			{
				query.CommandText = queryText;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, revision);
				return query.ExecuteQuery(x => x.GetCommit(this.serializer));
			});
		}

		public virtual void Persist(CommitAttempt uncommitted)
		{
			this.Execute(uncommitted.StreamId, cmd =>
			{
				var commit = uncommitted.ToCommit();

				cmd.CommandText = this.dialect.PersistCommitAttempt;
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.StreamName, uncommitted.StreamName);
				cmd.AddParameter(this.dialect.CommitId, commit.CommitId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				cmd.AddParameter(this.dialect.ExpectedRevision, uncommitted.PreviousStreamRevision);
				cmd.AddParameter(this.dialect.StreamRevision, commit.StreamRevision);
				cmd.AddParameter(this.dialect.Headers, this.serializer.Serialize(commit.Headers));
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(commit.Events));

				this.TryPersist(cmd);
			});
		}
		protected virtual void TryPersist(IDbCommand command)
		{
			try
			{
				var affectedRows = command.ExecuteNonQuery();
				if (affectedRows == 0)
					throw new ConcurrencyException();
			}
			catch (DbException e)
			{
				if (this.dialect.IsDuplicateException(e))
					throw new DuplicateCommitException(e.Message, e);

				throw;
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = this.dialect.GetUndispatchedCommits;
				return query.ExecuteQuery(x => x.GetCommit(this.serializer));
			});
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.Execute(commit.StreamId, cmd =>
			{
				cmd.CommandText = this.dialect.MarkCommitAsDispatched;
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				cmd.ExecuteAndSuppressExceptions();
			});
		}

		public virtual IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = this.dialect.GetStreamsRequiringSnaphots;
				query.AddParameter(this.dialect.Threshold, maxThreshold);
				return query.ExecuteQuery(record => (Guid)record[0]);
			});
		}
		public virtual void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
			this.Execute(streamId, cmd =>
			{
				cmd.CommandText = this.dialect.AppendSnapshotToCommit;
				cmd.AddParameter(this.dialect.StreamId, streamId);
				cmd.AddParameter(this.dialect.CommitSequence, commitSequence);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot));
				cmd.ExecuteAndSuppressExceptions();
			});
		}

		protected virtual T Execute<T>(Guid streamId, Func<IDbCommand, T> callback)
		{
			var results = default(T);
			this.Execute(streamId, command => { results = callback(command); });
			return results;
		}
		protected virtual void Execute(Guid streamId, Action<IDbCommand> execute)
		{
			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			using (var connection = this.factory.Open(streamId))
			using (var command = connection.CreateCommand())
			{
				try
				{
					execute(command);
				}
				catch (ConcurrencyException)
				{
					throw;
				}
				catch (DuplicateCommitException)
				{
					throw;
				}
				catch (Exception e)
				{
					throw new PersistenceException(e.Message, e);
				}

				scope.Complete();
			}
		}
	}
}