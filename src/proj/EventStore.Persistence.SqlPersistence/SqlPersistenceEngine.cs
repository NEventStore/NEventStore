namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Transactions;
	using Persistence;
	using Serialization;

	public class SqlPersistenceEngine : IPersistStreams
	{
		private readonly IConnectionFactory factory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlPersistenceEngine(IConnectionFactory factory, ISqlDialect dialect, ISerialize serializer)
		{
			this.factory = factory;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no-op
		}

		public virtual void Initialize()
		{
			this.Execute(Guid.Empty, statement =>
				statement.ExecuteWithSuppression(this.dialect.InitializeStorage));
		}

		public virtual IEnumerable<Commit> GetFromSnapshotUntil(Guid streamId, int maxRevision)
		{
			return this.Fetch(streamId, maxRevision, this.dialect.GetCommitsFromSnapshotUntilRevision);
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision)
		{
			return this.Fetch(streamId, minRevision, this.dialect.GetCommitsFromStartingRevision);
		}
		protected virtual IEnumerable<Commit> Fetch(Guid streamId, int revision, string queryText)
		{
			return this.Execute(streamId, query =>
			{
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, revision);
				return query.ExecuteWithQuery(queryText, x => x.GetCommit(this.serializer));
			});
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.Execute(Guid.Empty, query =>
			{
				var statement = this.dialect.GetCommitsFromInstant;
				query.AddParameter(this.dialect.CommitStamp, start);
				return query.ExecuteWithQuery(statement, x => x.GetCommit(this.serializer));
			});
		}

		public virtual void Persist(CommitAttempt uncommitted)
		{
			this.Execute(uncommitted.StreamId, cmd =>
			{
				var commit = uncommitted.ToCommit();

				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, commit.StreamRevision);
				cmd.AddParameter(this.dialect.Items, commit.Events.Count);
				cmd.AddParameter(this.dialect.CommitId, commit.CommitId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				cmd.AddParameter(this.dialect.CommitStamp, DateTime.UtcNow);
				cmd.AddParameter(this.dialect.Headers, this.serializer.Serialize(commit.Headers));
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(commit.Events));

				var rowsAffected = cmd.Execute(this.dialect.PersistCommitAttempt);
				if (rowsAffected == 0)
					throw new ConcurrencyException();
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.Execute(Guid.Empty, query =>
				query.ExecuteWithQuery(this.dialect.GetUndispatchedCommits, x => x.GetCommit(this.serializer)));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.Execute(commit.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				cmd.ExecuteWithSuppression(this.dialect.MarkCommitAsDispatched);
			});
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.Execute(Guid.Empty, query =>
			{
				var statement = this.dialect.GetStreamsRequiringSnaphots;
				query.AddParameter(this.dialect.Threshold, maxThreshold);
				return query.ExecuteWithQuery(statement, record => record.GetStreamToSnapshot());
			});
		}
		public virtual void AddSnapshot(Guid streamId, int streamRevision, object snapshot)
		{
			this.Execute(streamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, streamId);
				cmd.AddParameter(this.dialect.StreamRevision, streamRevision);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot));
				cmd.ExecuteWithSuppression(this.dialect.AppendSnapshotToCommit);
			});
		}

		protected virtual IEnumerable<T> Execute<T>(Guid streamId, Func<IDbStatement, IEnumerable<T>> executeQuery)
		{
			var scope = new TransactionScope(TransactionScopeOption.Suppress);
			IDbConnection connection = null;
			IDbTransaction transaction = null;
			IDbStatement query = null;

			try
			{
				connection = this.factory.Open(streamId);
				transaction = this.dialect.OpenTransaction(connection);
				query = this.dialect.BuildStatement(connection, transaction, scope);
				return executeQuery(query);
			}
			catch (Exception e)
			{
				if (query != null)
					query.Dispose();
				if (transaction != null)
					transaction.Dispose();
				if (connection != null)
					connection.Dispose();
				scope.Dispose();

				throw new PersistenceEngineException(e.Message, e);
			}
		}
		protected virtual void Execute(Guid streamId, Action<IDbStatement> execute)
		{
			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			using (var connection = this.factory.Open(streamId))
			using (var transaction = this.dialect.OpenTransaction(connection))
			using (var statement = this.dialect.BuildStatement(connection, transaction, scope))
			{
				try
				{
					execute(statement);

					if (transaction != null)
						transaction.Commit();
				}
				catch (Exception e)
				{
					if (e is ConcurrencyException || e is DuplicateCommitException)
						throw;

					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
	}
}