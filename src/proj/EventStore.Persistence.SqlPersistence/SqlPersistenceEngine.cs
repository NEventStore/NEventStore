namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Transactions;
	using Logging;
	using Persistence;
	using Serialization;

	public class SqlPersistenceEngine : IPersistStreams
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SqlPersistenceEngine));
		private static readonly DateTime EpochTime = new DateTime(1970, 1, 1);
		private readonly IConnectionFactory connectionFactory;
		private readonly ISqlDialect dialect;
		private readonly ISerialize serializer;
		private readonly TransactionScopeOption scopeOption;
		private readonly int pageSize;
		private int initialized;

		public SqlPersistenceEngine(
			IConnectionFactory connectionFactory,
			ISqlDialect dialect,
			ISerialize serializer,
			TransactionScopeOption scopeOption,
			int pageSize)
		{
			if (connectionFactory == null)
				throw new ArgumentNullException("connectionFactory");

			if (dialect == null)
				throw new ArgumentNullException("dialect");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			if (pageSize < 0)
				throw new ArgumentException("pageSize");

			this.connectionFactory = connectionFactory;
			this.dialect = dialect;
			this.serializer = serializer;
			this.scopeOption = scopeOption;
			this.pageSize = pageSize;

			Logger.Debug(Messages.UsingScope, this.scopeOption.ToString());
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			// no op: we want to be sure any async operations (if any) are able to complete successfully.
			Logger.Debug(Messages.ShuttingDownPersistence);
		}

		public virtual void Initialize()
		{
			if (Interlocked.Increment(ref this.initialized) > 1)
				return;

			Logger.Debug(Messages.InitializingStorage);
			this.ExecuteCommand(Guid.Empty, statement =>
				statement.ExecuteWithoutExceptions(this.dialect.InitializeStorage));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);
			return this.ExecuteQuery(streamId, query =>
			{
				var statement = this.dialect.GetCommitsFromStartingRevision;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, minRevision);
				query.AddParameter(this.dialect.MaxStreamRevision, maxRevision);
				query.AddParameter(this.dialect.CommitSequence, 0);
				return query.ExecutePagedQuery(statement,
					x => x.GetCommit(this.serializer),
					(q, c) => q.SetParameter(this.dialect.CommitSequence, c.CommitSequence),
					this.pageSize);
			});
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			start = start < EpochTime ? EpochTime : start;

			Logger.Debug(Messages.GettingAllCommitsFrom, start);
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetCommitsFromInstant;
				query.AddParameter(this.dialect.CommitStamp, start);
				query.AddParameter(this.dialect.StreamId, Guid.Empty);
				query.AddParameter(this.dialect.StreamRevision, 0);
				return query.ExecutePagedQuery(
					statement,
					x => x.GetCommit(this.serializer),
					(q, c) => q.SetParameter(this.dialect.StreamId, this.dialect.CoalesceParameterValue(c.StreamId))
						.SetParameter(this.dialect.StreamRevision, c.StreamRevision),
					this.pageSize);
			});
		}
		public virtual void Commit(Commit attempt)
		{
			Logger.Debug(Messages.AttemptingToCommit, 
				attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

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
				Logger.Debug(Messages.CommitPersisted, rowsAffected);

				if (rowsAffected > 0)
					return rowsAffected;

				throw new ConcurrencyException();
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			Logger.Debug(Messages.GettingUndispatchedCommits);
			return this.ExecuteQuery(Guid.Empty, query =>
				query.ExecuteWithQuery(this.dialect.GetUndispatchedCommits, x => x.GetCommit(this.serializer)));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);
			this.ExecuteCommand(commit.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, commit.StreamId);
				cmd.AddParameter(this.dialect.CommitSequence, commit.CommitSequence);
				return cmd.ExecuteWithoutExceptions(this.dialect.MarkCommitAsDispatched);
			});
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			Logger.Debug(Messages.GettingStreamsToSnapshot);
			return this.ExecuteQuery(Guid.Empty, query =>
			{
				var statement = this.dialect.GetStreamsRequiringSnapshots;
				query.AddParameter(this.dialect.StreamId, Guid.Empty);
				query.AddParameter(this.dialect.Threshold, maxThreshold);
				return query.ExecutePagedQuery(
					statement,
					x => x.GetStreamToSnapshot(),
					(q, s) => q.SetParameter(this.dialect.StreamId, this.dialect.CoalesceParameterValue(s.StreamId)),
					this.pageSize);
			});
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			Logger.Debug(Messages.GettingRevision, streamId, maxRevision);
			return this.ExecuteQuery(streamId, query =>
			{
				var statement = this.dialect.GetSnapshot;
				query.AddParameter(this.dialect.StreamId, streamId);
				query.AddParameter(this.dialect.StreamRevision, maxRevision);
				return query.ExecuteWithQuery(statement, x => x.GetSnapshot(this.serializer));
			}).FirstOrDefault();
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);
			return this.ExecuteCommand(snapshot.StreamId, cmd =>
			{
				cmd.AddParameter(this.dialect.StreamId, snapshot.StreamId);
				cmd.AddParameter(this.dialect.StreamRevision, snapshot.StreamRevision);
				cmd.AddParameter(this.dialect.Payload, this.serializer.Serialize(snapshot.Payload));
				return cmd.ExecuteWithoutExceptions(this.dialect.AppendSnapshotToCommit);
			}) > 0;
		}

		public virtual void Purge()
		{
			Logger.Warn(Messages.PurgingStorage);
			this.ExecuteCommand(Guid.Empty, cmd =>
				cmd.Execute(this.dialect.PurgeStorage));
		}

		protected virtual IEnumerable<T> ExecuteQuery<T>(Guid streamId, Func<IDbStatement, IEnumerable<T>> query)
		{
			var scope = this.OpenQueryScope();
			IDbConnection connection = null;
			IDbTransaction transaction = null;
			IDbStatement statement = null;

			try
			{
				connection = this.connectionFactory.OpenReplica(streamId);
				transaction = this.dialect.OpenTransaction(connection);
				statement = this.dialect.BuildStatement(connection, transaction);

				Logger.Verbose(Messages.ExecutingQuery);
				return query(statement).Yield(() =>
				{
					Logger.Verbose(Messages.QueryCompleted);
					Logger.Warn("Disposing scope");
					scope.Complete();
					scope.Dispose();
				});
			}
			catch (Exception e)
			{
				if (statement != null)
					statement.Dispose();
				if (transaction != null)
					transaction.Dispose();
				if (connection != null)
					connection.Dispose();
				if (scope != null)
					scope.Dispose();

				Logger.Warn("Disposing scope");
				Logger.Debug(Messages.StorageThrewException, e.GetType());
				if (e is StorageUnavailableException)
					throw;

				throw new StorageException(e.Message, e);
			}
		}
		protected virtual TransactionScope OpenQueryScope()
		{
			return this.OpenCommandScope() ?? new TransactionScope(TransactionScopeOption.Suppress);
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
					Logger.Verbose(Messages.ExecutingCommand);
					var rowsAffected = command(statement);
					Logger.Verbose(Messages.CommandExecuted, rowsAffected);

					if (transaction != null)
						transaction.Commit();

					Logger.Warn("Disposing scope");
					if (scope != null)
						scope.Complete();

					return rowsAffected;
				}
				catch (Exception e)
				{
					Logger.Debug(Messages.StorageThrewException, e.GetType());
					if (e is ConcurrencyException || e is DuplicateCommitException || e is StorageUnavailableException)
						throw;

					throw new StorageException(e.Message, e);
				}
			}
		}
		protected virtual TransactionScope OpenCommandScope()
		{
			Logger.Warn("Opening scope");
			return new TransactionScope(this.scopeOption);
		}
	}
}