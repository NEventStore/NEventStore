namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Transactions;
	using Persistence;
	using Serialization;

	public class SqlPersistenceEngine : IPersistStreams
	{
		private readonly IConnectionFactory connectionFactory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;
		private readonly TransactionScopeOption scopeOption;
		private int initialized;

		public SqlPersistenceEngine(
			IConnectionFactory connectionFactory,
			ISqlDialect dialect,
			ISerialize serializer,
			TransactionScopeOption scopeOption)
		{
			if (connectionFactory == null)
				throw new ArgumentNullException("connectionFactory");

			if (dialect == null)
				throw new ArgumentNullException("dialect");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.connectionFactory = connectionFactory;
			this.dialect = dialect;
			this.serializer = serializer;
			this.scopeOption = scopeOption;
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
			if (Interlocked.Increment(ref this.initialized) > 1)
				return;

			this.ExecuteCommand(Guid.Empty, statement =>
				statement.ExecuteWithoutExceptions(this.dialect.InitializeStorage));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.ExecuteQuery(streamId, query =>
			{
				var statement = this.dialect.GetCommitsFromStartingRevision;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, minRevision);
				query.AddParameter(this.dialect.MaxStreamRevision, maxRevision);
				return query.ExecutePagedQuery(statement, this.Transform);
			});
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetCommitsFromInstant;
				query.AddParameter(this.dialect.CommitStamp, start);
				return query.ExecutePagedQuery(statement, this.Transform);
			});
		}
		private Commit Transform(IDataRecord record)
		{
			return record.GetCommit(this.serializer);
		}

		public virtual void Commit(Commit attempt)
		{
			this.ExecuteCommand(attempt.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, attempt.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, attempt.StreamRevision);
				cmd.AddParameter(this.dialect.Items, attempt.Events.Count);
				cmd.AddParameter(this.dialect.CommitId, attempt.CommitId);
				cmd.AddParameter(this.dialect.CommitSequence, attempt.CommitSequence);
				cmd.AddParameter(this.dialect.CommitStamp, attempt.CommitStamp);
				cmd.AddParameter(this.dialect.Headers, this.serializer.Serialize(attempt.Headers));
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(attempt.Events));

				var rowsAffected = cmd.Execute(this.dialect.PersistCommit);
				if (rowsAffected <= 0)
					throw new ConcurrencyException();

				return rowsAffected;
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			var statement = this.dialect.GetUndispatchedCommits;
			return this.ExecuteQuery(Guid.Empty, query =>
				query.ExecutePagedQuery(statement, this.Transform));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.ExecuteCommand(commit.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				return cmd.ExecuteWithoutExceptions(this.dialect.MarkCommitAsDispatched);
			});
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetStreamsRequiringSnapshots;
				query.AddParameter(this.dialect.Threshold, maxThreshold);
				return query.ExecutePagedQuery(statement, x => x.GetStreamToSnapshot());
			});
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.ExecuteQuery(streamId, query =>
			{
				var statement = this.dialect.GetSnapshot;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, maxRevision);
				return query.ExecuteWithQuery(statement, x => x.GetSnapshot(this.serializer)).FirstOrDefault();
			});
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			return this.ExecuteCommand(snapshot.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, snapshot.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, snapshot.StreamRevision);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot.Payload));
				return cmd.ExecuteWithoutExceptions(this.dialect.AppendSnapshotToCommit);
			}) > 0;
		}

		protected virtual T ExecuteQuery<T>(Guid streamId, Func<IDbStatement, T> query)
		{
			var scope = this.OpenQueryScope();
			IDbConnection connection = null;
			IDbTransaction transaction = null;
			IDbStatement statement = null;

			try
			{
				connection = this.connectionFactory.OpenReplica(streamId);
				transaction = this.dialect.OpenTransaction(connection);
				statement = this.dialect.BuildStatement(connection, transaction, scope);
				return query(statement);
			}
			catch (Exception e)
			{
				if (statement != null)
					statement.Dispose();
				if (transaction != null)
					transaction.Dispose();
				if (connection != null)
					connection.Dispose();
				scope.Dispose();

				if (e is StorageUnavailableException)
					throw;

				throw new StorageException(e.Message, e);
			}
		}
		protected virtual int ExecuteCommand(Guid streamId, Func<IDbStatement, int> command)
		{
			using (var scope = this.OpenCommandScope())
			using (var connection = this.connectionFactory.OpenMaster(streamId))
			using (var transaction = this.dialect.OpenTransaction(connection))
			using (var statement = this.dialect.BuildStatement(connection, transaction, scope))
			{
				try
				{
					var rowsAffected = command(statement);
					if (transaction != null)
						transaction.Commit();

					if (scope != null)
						scope.Complete();

					return rowsAffected;
				}
				catch (Exception e)
				{
					if (e is ConcurrencyException || e is DuplicateCommitException || e is StorageUnavailableException)
						throw;

					throw new StorageException(e.Message, e);
				}
			}
		}
		protected virtual TransactionScope OpenQueryScope()
		{
			return this.OpenCommandScope();
		}
		protected virtual TransactionScope OpenCommandScope()
		{
			return new TransactionScope(this.scopeOption);
		}
	}
}