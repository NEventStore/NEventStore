namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Transactions;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class SqlPersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SqlPersistenceEngine));
        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1);
        private readonly IConnectionFactory _connectionFactory;
        private readonly ISqlDialect _dialect;
        private readonly int _pageSize;
        private readonly TransactionScopeOption _scopeOption;
        private readonly ISerialize _serializer;
        private bool _disposed;
        private int _initialized;

        public SqlPersistenceEngine(
            IConnectionFactory connectionFactory,
            ISqlDialect dialect,
            ISerialize serializer,
            TransactionScopeOption scopeOption,
            int pageSize)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            if (dialect == null)
            {
                throw new ArgumentNullException("dialect");
            }

            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            if (pageSize < 0)
            {
                throw new ArgumentException("pageSize");
            }

            _connectionFactory = connectionFactory;
            _dialect = dialect;
            _serializer = serializer;
            _scopeOption = scopeOption;
            _pageSize = pageSize;

            Logger.Debug(Messages.UsingScope, _scopeOption.ToString());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Initialize()
        {
            if (Interlocked.Increment(ref _initialized) > 1)
            {
                return;
            }

            Logger.Debug(Messages.InitializingStorage);
            ExecuteCommand(statement => statement.ExecuteWithoutExceptions(_dialect.InitializeStorage));
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);
            streamId = streamId.ToHash();
            return ExecuteQuery(query =>
                {
                    string statement = _dialect.GetCommitsFromStartingRevision;
                    query.AddParameter(_dialect.BucketId, bucketId);
                    query.AddParameter(_dialect.StreamId, streamId);
                    query.AddParameter(_dialect.StreamRevision, minRevision);
                    query.AddParameter(_dialect.MaxStreamRevision, maxRevision);
                    query.AddParameter(_dialect.CommitSequence, 0);
                    return query.ExecutePagedQuery(statement, (q, r) => q.SetParameter(_dialect.CommitSequence, r.CommitSequence()))
                            .Select(x => x.GetCommit(_serializer));
                });
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            start = start.AddTicks(-(start.Ticks%TimeSpan.TicksPerSecond)); // Rounds down to the nearest second.
            start = start < EpochTime ? EpochTime : start;

            Logger.Debug(Messages.GettingAllCommitsFrom, start, bucketId);
            return ExecuteQuery(query =>
                {
                    string statement = _dialect.GetCommitsFromInstant;
                    query.AddParameter(_dialect.BucketId, bucketId);
                    query.AddParameter(_dialect.CommitStamp, start);
                    return query.ExecutePagedQuery(statement, (q, r) => q.SetParameter(_dialect.CommitSequence, r.CommitSequence()))
                            .Select(x => x.GetCommit(_serializer));

                });
        }

        public ICheckpoint GetCheckpoint(string checkpointToken)
        {
            return string.IsNullOrWhiteSpace(checkpointToken) ? null : LongCheckpoint.Parse(checkpointToken);
        }

        public virtual IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            start = start.AddTicks(-(start.Ticks%TimeSpan.TicksPerSecond)); // Rounds down to the nearest second.
            start = start < EpochTime ? EpochTime : start;
            end = end < EpochTime ? EpochTime : end;

            Logger.Debug(Messages.GettingAllCommitsFromTo, start, end);
            return ExecuteQuery(query =>
                {
                    string statement = _dialect.GetCommitsFromToInstant;
                    query.AddParameter(_dialect.BucketId, bucketId);
                    query.AddParameter(_dialect.CommitStampStart, start);
                    query.AddParameter(_dialect.CommitStampEnd, end);
                    return query.ExecutePagedQuery(statement, (q, r) => { }).Select(x => x.GetCommit(_serializer));
                });
        }

        public virtual ICommit Commit(CommitAttempt attempt)
        {
            ICommit commit;
            try
            {
                commit = PersistCommit(attempt);
                Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
            }
            catch (Exception e)
            {
                if (!(e is UniqueKeyViolationException))
                {
                    throw;
                }

                if (DetectDuplicate(attempt))
                {
                    Logger.Info(Messages.DuplicateCommit);
                    throw new DuplicateCommitException(e.Message, e);
                }

                Logger.Info(Messages.ConcurrentWriteDetected);
                throw new ConcurrencyException(e.Message, e);
            }
            return commit;
        }

        public virtual IEnumerable<ICommit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);
            return
                ExecuteQuery(query => query.ExecutePagedQuery(_dialect.GetUndispatchedCommits, (q, r) => { }))
                    .Select(x => x.GetCommit(_serializer))
                    .ToArray(); // avoid paging
        }

        public virtual void MarkCommitAsDispatched(ICommit commit)
        {
            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);
            string streamId = commit.StreamId.ToHash();
            ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, commit.BucketId);
                    cmd.AddParameter(_dialect.StreamId, streamId);
                    cmd.AddParameter(_dialect.CommitSequence, commit.CommitSequence);
                    return cmd.ExecuteWithoutExceptions(_dialect.MarkCommitAsDispatched);
                });
        }

        public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot);
            return ExecuteQuery(query =>
                {
                    string statement = _dialect.GetStreamsRequiringSnapshots;
                    query.AddParameter(_dialect.BucketId, bucketId);
                    query.AddParameter(_dialect.Threshold, maxThreshold);
                    return
                        query.ExecutePagedQuery(statement,
                            (q, s) => q.SetParameter(_dialect.StreamId, _dialect.CoalesceParameterValue(s.StreamId())))
                            .Select(x => x.GetStreamToSnapshot());
                });
        }

        public virtual ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);
            streamId = streamId.ToHash();
            return ExecuteQuery(query =>
                {
                    string statement = _dialect.GetSnapshot;
                    query.AddParameter(_dialect.BucketId, bucketId);
                    query.AddParameter(_dialect.StreamId, streamId);
                    query.AddParameter(_dialect.StreamRevision, maxRevision);
                    return query.ExecuteWithQuery(statement).Select(x => x.GetSnapshot(_serializer));
                }).FirstOrDefault();
        }

        public virtual bool AddSnapshot(ISnapshot snapshot)
        {
            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);
            string streamId = snapshot.StreamId.ToHash();
            return ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, snapshot.BucketId);
                    cmd.AddParameter(_dialect.StreamId, streamId);
                    cmd.AddParameter(_dialect.StreamRevision, snapshot.StreamRevision);
                    cmd.AddParameter(_dialect.Payload, _serializer.Serialize(snapshot.Payload));
                    return cmd.ExecuteWithoutExceptions(_dialect.AppendSnapshotToCommit);
                }) > 0;
        }

        public virtual void Purge()
        {
            Logger.Warn(Messages.PurgingStorage);
            ExecuteCommand(cmd => cmd.ExecuteNonQuery(_dialect.PurgeStorage));
        }

        public void Purge(string bucketId)
        {
            Logger.Warn(Messages.PurgingBucket, bucketId);
            ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, bucketId);
                    return cmd.ExecuteNonQuery(_dialect.PurgeBucket);
                });
        }

        public void Drop()
        {
            Logger.Warn(Messages.DroppingTables);
            ExecuteCommand(cmd => cmd.ExecuteNonQuery(_dialect.Drop));
        }

        public void DeleteStream(string bucketId, string streamId)
        {
            Logger.Warn(Messages.DeletingStream, streamId, bucketId);
            streamId = streamId.ToHash();
            ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, bucketId);
                    cmd.AddParameter(_dialect.StreamId, streamId);
                    return cmd.ExecuteNonQuery(_dialect.DeleteStream);
                });
        }

        public IEnumerable<ICommit> GetFrom(string checkpointToken = null)
        {
            int checkpoint;
            bool b = int.TryParse(checkpointToken, out checkpoint);
            Guard.NotFalse(b, () => new ArgumentException("checkpointToken expected to be parsable to an int"));
            Logger.Debug(Messages.GettingAllCommitsFromCheckpoint, checkpointToken);
            return ExecuteQuery(query =>
            {
                string statement = _dialect.GetCommitsFromCheckpoint;
                query.AddParameter(_dialect.CheckpointNumber, checkpoint);
                return query.ExecutePagedQuery(statement, (q, r) => { }).Select(x => x.GetCommit(_serializer));
            });
        }

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            Logger.Debug(Messages.ShuttingDownPersistence);
            _disposed = true;
        }

        private ICommit PersistCommit(CommitAttempt attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence, attempt.BucketId);
            string streamId = attempt.StreamId.ToHash();
            return ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, attempt.BucketId);
                    cmd.AddParameter(_dialect.StreamId, streamId);
                    cmd.AddParameter(_dialect.StreamIdOriginal, attempt.StreamId);
                    cmd.AddParameter(_dialect.StreamRevision, attempt.StreamRevision);
                    cmd.AddParameter(_dialect.Items, attempt.Events.Count);
                    cmd.AddParameter(_dialect.CommitId, attempt.CommitId);
                    cmd.AddParameter(_dialect.CommitSequence, attempt.CommitSequence);
                    cmd.AddParameter(_dialect.CommitStamp, attempt.CommitStamp);
                    cmd.AddParameter(_dialect.Headers, _serializer.Serialize(attempt.Headers));
                    cmd.AddParameter(_dialect.Payload, _serializer.Serialize(attempt.Events.ToList()));
                    var checkpointNumber = cmd.ExecuteScalar(_dialect.PersistCommit).ToLong();
                    return new Commit(
                        attempt.BucketId,
                        attempt.StreamId,
                        attempt.StreamRevision,
                        attempt.CommitId,
                        attempt.CommitSequence,
                        attempt.CommitStamp,
                        checkpointNumber.ToString(CultureInfo.InvariantCulture),
                        attempt.Headers,
                        attempt.Events);
                });
        }

        private bool DetectDuplicate(CommitAttempt attempt)
        {
            string streamId = attempt.StreamId.ToHash();
            return ExecuteCommand(cmd =>
                {
                    cmd.AddParameter(_dialect.BucketId, attempt.BucketId);
                    cmd.AddParameter(_dialect.StreamId, streamId);
                    cmd.AddParameter(_dialect.CommitId, attempt.CommitId);
                    cmd.AddParameter(_dialect.CommitSequence, attempt.CommitSequence);
                    object value = cmd.ExecuteScalar(_dialect.DuplicateCommit);
                    return (value is long ? (long) value : (int) value) > 0;
                });
        }

        protected virtual IEnumerable<T> ExecuteQuery<T>(Func<IDbStatement, IEnumerable<T>> query)
        {
            ThrowWhenDisposed();

            TransactionScope scope = OpenQueryScope();
            IDbConnection connection = null;
            IDbTransaction transaction = null;
            IDbStatement statement = null;

            try
            {
                connection = _connectionFactory.Open();
                transaction = _dialect.OpenTransaction(connection);
                statement = _dialect.BuildStatement(scope, connection, transaction);
                statement.PageSize = _pageSize;

                Logger.Verbose(Messages.ExecutingQuery);
                return query(statement);
            }
            catch (Exception e)
            {
                if (statement != null)
                {
                    statement.Dispose();
                }
                if (transaction != null)
                {
                    transaction.Dispose();
                }
                if (connection != null)
                {
                    connection.Dispose();
                }
                if (scope != null)
                {
                    scope.Dispose();
                }

                Logger.Debug(Messages.StorageThrewException, e.GetType());
                if (e is StorageUnavailableException)
                {
                    throw;
                }

                throw new StorageException(e.Message, e);
            }
        }

        protected virtual TransactionScope OpenQueryScope()
        {
            return OpenCommandScope() ?? new TransactionScope(TransactionScopeOption.Suppress);
        }

        private void ThrowWhenDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            Logger.Warn(Messages.AlreadyDisposed);
            throw new ObjectDisposedException(Messages.AlreadyDisposed);
        }

        protected virtual T ExecuteCommand<T>(Func<IDbStatement, T> command)
        {
            ThrowWhenDisposed();

            using (TransactionScope scope = OpenCommandScope())
            using (IDbConnection connection = _connectionFactory.Open())
            using (IDbTransaction transaction = _dialect.OpenTransaction(connection))
            using (IDbStatement statement = _dialect.BuildStatement(scope, connection, transaction))
            {
                try
                {
                    Logger.Verbose(Messages.ExecutingCommand);
                    T rowsAffected = command(statement);
                    Logger.Verbose(Messages.CommandExecuted, rowsAffected);

                    if (transaction != null)
                    {
                        transaction.Commit();
                    }

                    if (scope != null)
                    {
                        scope.Complete();
                    }

                    return rowsAffected;
                }
                catch (Exception e)
                {
                    Logger.Debug(Messages.StorageThrewException, e.GetType());
                    if (!RecoverableException(e))
                    {
                        throw new StorageException(e.Message, e);
                    }

                    Logger.Info(Messages.RecoverableExceptionCompletesScope);
                    if (scope != null)
                    {
                        scope.Complete();
                    }

                    throw;
                }
            }
        }

        protected virtual TransactionScope OpenCommandScope()
        {
            return new TransactionScope(_scopeOption);
        }

        private static bool RecoverableException(Exception e)
        {
            return e is UniqueKeyViolationException || e is StorageUnavailableException;
        }
    }

    internal static class StreamIdHashExtensions
    {
        internal static string ToHash(this string streamId)
        {
            byte[] hashBytes = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(streamId));
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }
}