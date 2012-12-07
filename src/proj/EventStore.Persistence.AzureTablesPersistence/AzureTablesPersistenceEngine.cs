using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.Linq;
using System.Threading;
using EventStore.Logging;
using EventStore.Persistence.AzureTablesPersistence.Datastructures;
using EventStore.Persistence.AzureTablesPersistence.Extensions;
using EventStore.Serialization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence
{
    public class AzureTablesPersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AzureTablesPersistenceEngine));
        private readonly CloudTableClient tableClient;
        private readonly ISerialize serializer;

        private int initialized;
        private bool disposed;

        public AzureTablesPersistenceEngine(CloudTableClient tableClient, ISerialize serializer)
        {
            if (tableClient == null)
                throw new ArgumentNullException("tableClient");

            if (serializer == null)
                throw new ArgumentNullException("serializer");

            this.tableClient = tableClient;
            this.serializer = serializer;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this.disposed)
                return;

            Logger.Debug(Messages.ShuttingDownPersistence);
            this.disposed = true;
        }

        public void Initialize()
        {
            if (Interlocked.Increment(ref initialized) > 1)
                return; // Initialization already done.

            PersistedCommits.CreateIfNotExists();
            PersistedSnapshots.CreateIfNotExists();
            PersistedStreamHeads.CreateIfNotExists();
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);

            var stringStreamId = streamId.ToString();
            var minRowKey = IntegralRowKeyHelpers.EncodeDouble(minRevision);
            var maxRowKey = IntegralRowKeyHelpers.EncodeDouble(maxRevision);

            var partitionKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, stringStreamId);
            var minRowKeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, minRowKey);
            var maxRowKeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, maxRowKey);
            var rowKeyFilter = TableQuery.CombineFilters(minRowKeyFilter, TableOperators.And, maxRowKeyFilter);

            var filter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, rowKeyFilter);
            var query = new TableQuery<AzureCommit>();
            query = query.Where(filter);

            var commits = ExecuteQuery(query);

            return commits.Select(x => x.ToCommit(serializer));
        }
        public IEnumerable<Commit> GetFrom(DateTime start)
        {
            Logger.Debug(Messages.GettingAllCommitsFrom, start);

            var query = new TableQuery<AzureCommit>();
            query = query.Where(TableQuery.GenerateFilterConditionForDate("CommitStamp", QueryComparisons.GreaterThanOrEqual, start));

            var commits = ExecuteQuery(query)
                                      .OrderBy(x => x.CommitStamp)
                                      .Select(x => x.ToCommit(serializer));

            return commits;
        }
        public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, start, end);

            var query = new TableQuery<AzureCommit>();

            var commitsAfterFilter = TableQuery.GenerateFilterConditionForDate("CommitStamp",
                                                                               QueryComparisons.GreaterThanOrEqual,
                                                                               start);

            var commitsBeforeFilter = TableQuery.GenerateFilterConditionForDate("CommitStamp",
                                                                                QueryComparisons.LessThanOrEqual,
                                                                                end);

            query = query.Where(TableQuery.CombineFilters(commitsAfterFilter, TableOperators.And, commitsBeforeFilter));

            var commits = ExecuteQuery(query)
                                      .OrderBy(x => x.CommitStamp)
                                      .Select(x => x.ToCommit(serializer));

            return commits;
        }


        public void Commit(Commit attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

            var commit = attempt.ToAzureTablesCommit(serializer);

            try
            {
                ExecuteTableOperation<AzureCommit>(TableOperation.Insert(commit));
                UpdateStreamHead(attempt.StreamId, attempt.StreamRevision, attempt.Events.Count);

                Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 409) // 409 == Conflict
                {
                    // Commit already exists!
                    var commitAlreadyStored = ExecutePointQuery<AzureCommit>(attempt.ToPointQuery());

                    if (commitAlreadyStored.CommitId == attempt.CommitId)
                        throw new DuplicateCommitException();

                    Logger.Debug(Messages.ConcurrentWriteDetected);
                    throw new ConcurrencyException();
                }

                throw;
            }
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);

            var query = new TableQuery<AzureCommit>();

            var notDispatchedFilter = TableQuery.GenerateFilterConditionForBool("Dispatched",
                                                                                QueryComparisons.Equal,
                                                                                false);

            query = query.Where(notDispatchedFilter);

            var commits = ExecuteQuery(query).Select(x => x.ToCommit(serializer));

            return commits;

        }
        public void MarkCommitAsDispatched(Commit commit)
        {
            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);

            var pointQuery = commit.ToPointQuery();

            bool committed = false;
            while (!committed)
            {
                var storedCommit = ExecutePointQuery<AzureCommit>(pointQuery);

                try
                {
                    storedCommit.Dispatched = true;

                    ExecuteTableOperation<AzureCommit>(TableOperation.Replace(storedCommit));

                    committed = true;
                }
                catch (DataServiceRequestException)
                {
                    Logger.Debug(Messages.RetryMarkingCommitAsDispatched, commit.CommitId);
                    // Retry. Not volatile since this is an idempotent operation.
                }
            }
        }


        public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot);

            var query = new TableQuery<AzureStreamHead>();
            query = query.Where(TableQuery.GenerateFilterConditionForInt("Unsnapshotted",
                                                                         QueryComparisons.GreaterThanOrEqual,
                                                                         maxThreshold));

            var streamHeads = ExecuteQuery(query)
                .OrderByDescending(x => x.Unsnapshotted)
                .Select(x => x.ToStreamHead());

            return streamHeads;
        }
        public Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

            var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey",
                                                                     QueryComparisons.Equal,
                                                                     streamId.ToString());

            var rowFilter = TableQuery.GenerateFilterCondition("RowKey",
                                                               QueryComparisons.LessThanOrEqual,
                                                               IntegralRowKeyHelpers.EncodeDouble(maxRevision));

            var filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            var query = new TableQuery<AzureSnapshot>();
            query.Where(filter);

            var snapshot = ExecuteQuery(query)
                              .OrderByDescending(x => x.GetStreamRevision())
                              .Select(x => x.ToSnapshot(serializer))
                              .FirstOrDefault();

            return snapshot;
        }
        public bool AddSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
                return false;

            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

            try
            {
                var azureSnapshot = snapshot.ToAzureSnapshot(serializer);
                var upsert = TableOperation.InsertOrReplace(azureSnapshot);
                ExecuteTableOperation<AzureSnapshot>(upsert);

                var query = snapshot.StreamId.ToStreamHeadPointQuery();
                var azureStreamHead = ExecutePointQuery<AzureStreamHead>(query);
                var unsnapshotted = azureStreamHead.HeadRevision - snapshot.StreamRevision;

                azureStreamHead.Unsnapshotted = unsnapshotted;
                azureStreamHead.SnapshotRevision = snapshot.StreamRevision;
                ExecuteTableOperation<AzureStreamHead>(TableOperation.Replace(azureStreamHead));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Purge()
        {
            Logger.Warn(Messages.PurgingStorage);

            PersistedCommits.DeleteIfExists();
            PersistedSnapshots.DeleteIfExists();
            PersistedStreamHeads.DeleteIfExists();
        }

        private void UpdateStreamHead(Guid streamId, int streamRevision, int eventCount)
        {
            var streamHead = new StreamHead(streamId, streamRevision, 0);

            var pointQuery = streamHead.ToPointQuery();
            var storedStreamHead = ExecutePointQuery<AzureStreamHead>(pointQuery);

            if (storedStreamHead == null)
            {
                storedStreamHead = streamHead.ToAzureTablesStreamHead();
                storedStreamHead.Unsnapshotted = eventCount;
                ExecuteTableOperation<AzureStreamHead>(TableOperation.Insert(storedStreamHead));
            }
            else
            {
                //TODO: Not sure if retrying like this should be done.
                bool committed = false;
                while (!committed)
                {
                    try
                    {
                        storedStreamHead.HeadRevision = streamRevision;
                        storedStreamHead.Unsnapshotted += eventCount;

                        ExecuteTableOperation<AzureStreamHead>(TableOperation.Replace(storedStreamHead));
                        committed = true;
                    }
                    catch (Microsoft.WindowsAzure.Storage.StorageException)
                    {
                        storedStreamHead = ExecutePointQuery<AzureStreamHead>(pointQuery);
                    }
                }
            }
        }

        private void ExecuteTableOperation<T>(TableOperation operation)
            where T : ITableEntity, new()
        {
            ThrowWhenDisposed();

            var table = GetTableForType(typeof(T));
 
            table.Execute(operation);
        }

        private IEnumerable<T> ExecuteQuery<T>(TableQuery<T> query)
            where T : ITableEntity, new()
        {
            ThrowWhenDisposed();

            var table = GetTableForType(typeof(T));

            return table.ExecuteQuery(query);
        }

        private T ExecutePointQuery<T>(TableOperation pointQuery)
            where T : ITableEntity, new()
        {
            ThrowWhenDisposed();

            var table = GetTableForType(typeof(T));

            return (T)table.Execute(pointQuery).Result;
        }

        private CloudTable GetTableForType(Type type)
        {
            if (type == typeof(AzureCommit))
                return PersistedCommits;

            if (type == typeof(AzureStreamHead))
                return PersistedStreamHeads;

            if (type == typeof(AzureSnapshot))
                return PersistedSnapshots;

            throw new ArgumentOutOfRangeException("type");
        }

        private void ThrowWhenDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("Object already disposed.");
        }

        private const string CommitsTableName = "Commits";
        private const string SnapshotsTableName = "Snapshots";
        private const string StreamHeadsTableName = "StreamHeads";

        protected CloudTable PersistedCommits
        {
            get { return tableClient.GetTableReference(CommitsTableName); }
        }

        protected CloudTable PersistedStreamHeads
        {
            get { return tableClient.GetTableReference(StreamHeadsTableName); }
        }

        protected CloudTable PersistedSnapshots
        {
            get { return tableClient.GetTableReference(SnapshotsTableName); }
        }

    }
}
