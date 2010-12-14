namespace EventStore.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
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

		public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return this.ReadStream(streamId, maxRevision, SqlStatements.GetUntil);
		}
		public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			return this.ReadStream(streamId, minRevision, SqlStatements.GetFrom);
		}
		private IEnumerable<Commit> ReadStream(Guid streamId, long revision, string sqlStatement)
		{
			return this.Execute(streamId, query =>
			{
				query.CommandText = sqlStatement;
				query.AddParameter(SqlParameters.StreamId, streamId);
				query.AddParameter(SqlParameters.OldRevision, revision);
				return query.ExecuteQuery(this.GetCommitFromRecord);
			});
		}
		private Commit GetCommitFromRecord(IDataRecord record)
		{
			// TODO
			return new Commit(
				(Guid)record[0],
				(Guid)record[1],
				(long)record[2],
				(long)record[3],
				null,
				null,
				this.serializer.Deserialize((byte[])record[6]));
		}

		public void Persist(CommitAttempt uncommitted)
		{
			this.Execute(uncommitted.StreamId, cmd =>
			{
				cmd.CommandText = SqlStatements.Persist;
				cmd.AddParameter(SqlParameters.StreamId, uncommitted.StreamId);
				cmd.AddParameter(SqlParameters.StreamName, uncommitted.StreamName);
				cmd.AddParameter(SqlParameters.CommitId, uncommitted.CommitId);
				cmd.AddParameter(SqlParameters.CommitSequence, uncommitted.CommitSequence());
				cmd.AddParameter(SqlParameters.OldRevision, uncommitted.PreviousCommitSequence);
				cmd.AddParameter(SqlParameters.NewRevision, uncommitted.NewRevision());
				cmd.AddParameter(SqlParameters.Payload, null); // TODO

				cmd.ExecuteNonQuery(); // catch duplicate persists, concurrency exceptions, etc.
			});
		}

		public IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = SqlStatements.GetUndispatched;
				return query.ExecuteQuery(this.GetCommitFromRecord);
			});
		}
		public void MarkCommitAsDispatched(Commit commit)
		{
			this.Execute(commit.StreamId, cmd =>
			{
				cmd.CommandText = SqlStatements.MarkAsDispatched;
				cmd.AddParameter(SqlParameters.StreamId, commit.StreamId);
				cmd.AddParameter(SqlParameters.CommitSequence, commit.CommitSequence);
				cmd.ExecuteNonQuery(); // TODO: make this so that it never throws
			});
		}

		public IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.Execute(Guid.Empty, query =>
			{
				query.CommandText = SqlStatements.GetStreamsToSnapshot;
				query.AddParameter(SqlParameters.Threshold, maxThreshold);
				return query.ExecuteQuery(record => (Guid)record[0]);
			});
		}
		public void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
			this.Execute(streamId, cmd =>
			{
				cmd.CommandText = SqlStatements.AddSnapshot;
				cmd.AddParameter(SqlParameters.StreamId, streamId);
				cmd.AddParameter(SqlParameters.CommitSequence, commitSequence);
				cmd.AddParameter(SqlParameters.Payload, this.serializer.Serialize(snapshot));
				cmd.ExecuteNonQuery(); // TODO: make this so that it never throws
			});
		}

		private T Execute<T>(Guid streamId, Func<IDbCommand, T> callback)
		{
			var results = default(T);
			this.Execute(streamId, command => { results = callback(command); });
			return results;
		}
		private void Execute(Guid streamId, Action<IDbCommand> callback)
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
					throw new PersistenceException(string.Empty, e); // TODO: message
				}

				scope.Complete();
			}
		}
	}
}