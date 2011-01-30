namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
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

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.Execute(streamId, query =>
			{
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, minRevision);
				query.AddParameter(this.dialect.MaxStreamRevision, maxRevision);
				return query.ExecuteWithQuery(this.dialect.GetCommitsFromStartingRevision, x => x.GetCommit(this.serializer));
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

		public virtual void Commit(Commit attempt)
		{
			this.Execute(attempt.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, attempt.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, attempt.StreamRevision);
				cmd.AddParameter(this.dialect.Items, attempt.Events.Count);
				cmd.AddParameter(this.dialect.CommitId, attempt.CommitId);
				cmd.AddParameter(this.dialect.CommitSequence, attempt.CommitSequence);
				cmd.AddParameter(this.dialect.CommitStamp, DateTime.UtcNow);
				cmd.AddParameter(this.dialect.Headers, this.serializer.Serialize(attempt.Headers));
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(attempt.Events));

				var rowsAffected = cmd.Execute(this.dialect.PersistCommit);
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
		public Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			Snapshot snapshot = null;
			this.Execute(streamId, query =>
			{
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, maxRevision);
				snapshot = query.ExecuteWithQuery(
					this.dialect.GetSnapshot, x => x.GetSnapshot(this.serializer)).FirstOrDefault();
			});
			return snapshot;
		}
		public void AddSnapshot(Snapshot snapshot)
		{
			this.Execute(snapshot.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, snapshot.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, snapshot.StreamRevision);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot.Payload));
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

				throw new StorageException(e.Message, e);
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

					throw new StorageException(e.Message, e);
				}
			}
		}
	}
}