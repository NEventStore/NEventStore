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
		private readonly ISerialize serializer;

		public SqlPersistence(IConnectionFactory factory, ISerialize serializer)
		{
			this.factory = factory;
			this.serializer = serializer;
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return this.Fetch(streamId, maxRevision, SqlStatements.ReadFromSnapshotUntil);
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return this.Fetch(streamId, minRevision, SqlStatements.GetFromStartingRevision);
		}
		private IEnumerable<Commit> Fetch(Guid streamId, long revision, string queryText)
		{
			return this.Execute(streamId, query =>
			{
				query.CommandText = queryText;
				query.AddParameter(SqlParameters.StreamId, streamId);
				query.AddParameter(SqlParameters.Revision, revision);
				return query.ExecuteQuery(x => x.GetCommit(this.serializer));
			});
		}

		public virtual void Persist(CommitAttempt uncommitted)
		{
			this.Execute(uncommitted.StreamId, cmd =>
			{
				var commit = uncommitted.ToCommit();

				cmd.CommandText = SqlStatements.PersistCommitAttempt;
				cmd.AddParameter(SqlParameters.StreamId, commit.StreamId);
				cmd.AddParameter(SqlParameters.StreamName, uncommitted.StreamName);
				cmd.AddParameter(SqlParameters.CommitId, commit.CommitId);
				cmd.AddParameter(SqlParameters.CommitSequence, commit.CommitSequence);
				cmd.AddParameter(SqlParameters.OldRevision, uncommitted.PreviousCommitSequence);
				cmd.AddParameter(SqlParameters.Revision, commit.StreamRevision);
				cmd.AddParameter(SqlParameters.Payload, this.serializer.Serialize(commit));

				TryPersist(cmd);
			});
		}
		private static void TryPersist(IDbCommand command)
		{
			try
			{
				var affectedRows = command.ExecuteNonQuery();
				if (affectedRows == 0)
					throw new ConcurrencyException();
			}
			catch (DbException e)
			{
				if (e.IsDuplicateKeyException())
					throw new DuplicateCommitException(e.Message, e);

				throw;
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = SqlStatements.GetUndispatchedCommits;
				return query.ExecuteQuery(x => x.GetCommit(this.serializer));
			});
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.Execute(commit.StreamId, cmd =>
			{
				cmd.CommandText = SqlStatements.MarkCommitAsDispatched;
				cmd.AddParameter(SqlParameters.StreamId, commit.StreamId);
				cmd.AddParameter(SqlParameters.CommitSequence, commit.CommitSequence);
				cmd.ExecuteAndSuppressExceptions();
			});
		}

		public virtual IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = SqlStatements.GetStreamsToSnapshot;
				query.AddParameter(SqlParameters.Threshold, maxThreshold);
				return query.ExecuteQuery(record => (Guid)record[0]);
			});
		}
		public virtual void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
			this.Execute(streamId, cmd =>
			{
				cmd.CommandText = SqlStatements.AddSnapshotToCommit;
				cmd.AddParameter(SqlParameters.StreamId, streamId);
				cmd.AddParameter(SqlParameters.CommitSequence, commitSequence);
				cmd.AddParameter(SqlParameters.Payload, this.serializer.Serialize(snapshot));
				cmd.ExecuteAndSuppressExceptions();
			});
		}

		protected virtual T Execute<T>(Guid streamId, Func<IDbCommand, T> callback)
		{
			var results = default(T);
			this.Execute(streamId, command => { results = callback(command); });
			return results;
		}
		protected virtual void Execute(Guid streamId, Action<IDbCommand> callback)
		{
			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			using (var connection = this.factory.Open(streamId))
			using (var command = connection.CreateCommand())
			{
				try
				{
					callback(command);
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