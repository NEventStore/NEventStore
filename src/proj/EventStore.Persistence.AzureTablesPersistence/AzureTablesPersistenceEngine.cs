using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using EventStore.Logging;
using EventStore.Serialization;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace EventStore.Persistence.AzureTablesPersistence
{
    public class AzureTablesPersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AzureTablesPersistenceEngine));

        private const string CommitsTableName = "Commits";
        private const string SnapshotsTableName = "Snapshots";
        private const string StreamHeadsTableName = "StreamHeads";


        private readonly CloudTableClient _tableClient;
        private readonly ISerialize _serializer;

        private int initialized;
        private bool disposed;

        public AzureTablesPersistenceEngine(CloudTableClient tableClient, ISerialize serializer)
        {
            if (tableClient == null)
                throw new ArgumentNullException("tableClient");

            if (serializer == null)
                throw new ArgumentNullException("serializer");

            _tableClient = tableClient;
            _serializer = serializer;
        }

        private CloudTable CommitsTable
        {
            get { return _tableClient.GetTableReference(CommitsTableName); }
        }

        private CloudTable StreamHeadsTable
        {
            get { return _tableClient.GetTableReference(StreamHeadsTableName); }
        }

        private CloudTable SnapShotsTable
        {
            get { return _tableClient.GetTableReference(SnapshotsTableName); }
        }

        public IEnumerable<Commit> GetFrom(DateTime start)
        {
            Logger.Debug(Messages.GettingAllCommitsFrom, start);

            var query = new TableQuery<AzureTablesCommit>();
            query = query.Where(TableQuery.GenerateFilterConditionForDate("CommitStamp", QueryComparisons.GreaterThanOrEqual, start));

            var commits = CommitsTable.ExecuteQuery(query)
                                      .OrderBy(x => x.CommitStamp)
                                      .Select(x => x.ToCommit(_serializer));

            return commits;
        }

        public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, start, end);

            var query = new TableQuery<AzureTablesCommit>();

            var commitsAfterFilter = TableQuery.GenerateFilterConditionForDate("CommitStamp",
                                                                               QueryComparisons.GreaterThanOrEqual,
                                                                               start);

            var commitsBeforeFilter = TableQuery.GenerateFilterConditionForDate("CommitStamp",
                                                                                QueryComparisons.LessThanOrEqual,
                                                                                end);

            query = query.Where(TableQuery.CombineFilters(commitsAfterFilter, TableOperators.And, commitsBeforeFilter));

            var commits = CommitsTable.ExecuteQuery(query)
                                      .OrderBy(x => x.CommitStamp)
                                      .Select(x => x.ToCommit(_serializer));

            return commits;
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            Logger.Debug(Messages.GettingUndispatchedCommits);

            var query = new TableQuery<AzureTablesCommit>();

            var notDispatchedFilter = TableQuery.GenerateFilterConditionForBool("Dispatched",
                                                                                QueryComparisons.Equal,
                                                                                false);

            query.Where(notDispatchedFilter);

            var commits = CommitsTable.ExecuteQuery(query)
                                      .Select(x => x.ToCommit(_serializer));

            return commits;

        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId);

            var pointQuery = commit.ToPointQuery();

            bool committed = false;
            while (!committed)
            {
                var storedCommit = (AzureTablesCommit)CommitsTable.Execute(pointQuery).Result;

                if (storedCommit != null)
                {
                    try
                    {
                        storedCommit.Dispatched = true;

                        var updateOperation = TableOperation.Replace(storedCommit);

                        CommitsTable.Execute(updateOperation);

                        committed = true;
                    }
                    catch (DataServiceRequestException)
                    {
                        Logger.Debug(Messages.RetryMarkingCommitAsDispatched, commit.CommitId);
                        // Retry. Not volatile since this is an idempotent operation.
                    }
                }
                else
                {
                    // Does not happen???
                    committed = true;
                }
            }
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);

            var stringStreamId = streamId.ToString();
            var minRowKey = minRevision.ToString(CultureInfo.InvariantCulture);
            var maxRowKey = maxRevision.ToString(CultureInfo.InvariantCulture);

            var partitionKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, stringStreamId);
            var minRowKeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, minRowKey);
            var maxRowKeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, maxRowKey);
            var rowKeyFilter = TableQuery.CombineFilters(minRowKeyFilter, TableOperators.And, maxRowKeyFilter);

            var filter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, rowKeyFilter);
            var commits = CommitsTable.ExecuteQuery(new TableQuery<AzureTablesCommit>() { FilterString = filter }).Select(x => x.ToCommit(_serializer));

            return commits;
        }

        public void Commit(Commit attempt)
        {
            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

            var commit = attempt.ToAzureTablesCommit(_serializer);

            try
            {
                CommitsTable.Execute(TableOperation.Insert(commit));
                UpdateStreamHead(attempt.StreamId, attempt.StreamRevision, attempt.Events.Count);

                Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 409) // 409 == Conflict
                {
                    // Commit already exists!

                    var commitAlreadyStored = (AzureTablesCommit)CommitsTable.Execute(attempt.ToPointQuery()).Result;

                    if (commitAlreadyStored.CommitId == attempt.CommitId)
                        throw new DuplicateCommitException();

                    Logger.Debug(Messages.ConcurrentWriteDetected);
                    throw new ConcurrencyException();
                }

                throw;
            }
        }

        private void UpdateStreamHead(Guid streamId, int streamRevision, int eventCount)
        {
            var streamHead = new StreamHead(streamId, streamRevision, 0);

            var pointQuery = streamHead.ToPointQuery();

            var storedStreamHead = (AzureTablesStreamHead)StreamHeadsTable.Execute(pointQuery).Result;


            if (storedStreamHead == null)
            {
                var result = StreamHeadsTable.Execute(TableOperation.Insert(new AzureTablesStreamHead()
                                                                   {
                                                                       PartitionKey =
                                                                           StreamHeadExtensions.GetPartitionKey(
                                                                               streamHead),
                                                                       RowKey =
                                                                           StreamHeadExtensions.GetRowKey(streamHead),
                                                                       HeadRevision = streamRevision,
                                                                       SnapshotRevision = 0,
                                                                       Unsnapshotted = eventCount
                                                                   }));
            }
            else
            {
                bool committed = false;
                while (!committed)
                {
                    try
                    {
                        storedStreamHead.HeadRevision = streamRevision;
                        storedStreamHead.Unsnapshotted += eventCount;

                        StreamHeadsTable.Execute(TableOperation.Replace(storedStreamHead));

                        committed = true;
                    }
                    catch (Microsoft.WindowsAzure.Storage.StorageException)
                    {
                        storedStreamHead = (AzureTablesStreamHead)StreamHeadsTable.Execute(pointQuery).Result;
                    }
                }
            }


        }

        public bool AddSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
                return false;

            Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

            try
            {
                var azureSnapshot = snapshot.ToAzureTablesSnapshot(_serializer);

                var upsert = TableOperation.InsertOrReplace(azureSnapshot);

                SnapShotsTable.Execute(upsert);

                var streamHead = new StreamHead(snapshot.StreamId, -1, -1);

                var pointQuery = TableOperation.Retrieve<AzureTablesStreamHead>(StreamHeadExtensions.GetPartitionKey(streamHead),
                                                                                StreamHeadExtensions.GetRowKey(streamHead));

                var azureStreamHead = (AzureTablesStreamHead)StreamHeadsTable.Execute(pointQuery).Result;

                var unsnapshotted = streamHead.HeadRevision - snapshot.StreamRevision;

                azureStreamHead.Unsnapshotted = unsnapshotted;
                azureStreamHead.SnapshotRevision = snapshot.StreamRevision;

                StreamHeadsTable.Execute(TableOperation.Replace(azureStreamHead));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

            var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey",
                                                                     QueryComparisons.Equal,
                                                                     streamId.ToString());

            var rowFilter = TableQuery.GenerateFilterCondition("RowKey",
                                                               QueryComparisons.LessThanOrEqual,
                                                               maxRevision.ToString(CultureInfo.InvariantCulture));

            var filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            var query = new TableQuery<AzureTablesSnapshot>();
            query.Where(filter);

            var snapshot = SnapShotsTable.ExecuteQuery(query)
                              .Select(x => x.ToSnapshot(_serializer))
                              .OrderByDescending(x => x.StreamRevision)
                              .FirstOrDefault();

            return snapshot;
        }

        public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            Logger.Debug(Messages.GettingStreamsToSnapshot);

            var query = new TableQuery<AzureTablesStreamHead>();
            query = query.Where(TableQuery.GenerateFilterConditionForInt("Unsnapshotted",
                                                                         QueryComparisons.GreaterThanOrEqual,
                                                                         maxThreshold));

            var streamHeads = StreamHeadsTable.ExecuteQuery(query).OrderByDescending(x => x.Unsnapshotted).Select(x => x.ToStreamHead());

            return streamHeads;
        }


        #region Initialization / Cleanup
        public void Initialize()
        {
            if (Interlocked.Increment(ref initialized) > 1)
                return; // Initialization already done.

            _tableClient.GetTableReference(CommitsTableName).CreateIfNotExists();
            _tableClient.GetTableReference(SnapshotsTableName).CreateIfNotExists();
            _tableClient.GetTableReference(StreamHeadsTableName).CreateIfNotExists();
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

        public void Purge()
        {
            Logger.Warn(Messages.PurgingStorage);

            _tableClient.GetTableReference(CommitsTableName).DeleteIfExists();
            _tableClient.GetTableReference(SnapshotsTableName).DeleteIfExists();
            _tableClient.GetTableReference(StreamHeadsTableName).DeleteIfExists();
        }
        #endregion

        private IQueryable<AzureTablesCommit> GetAzureTablesCommitsQuery(TableServiceContext context)
        {
            return context.CreateQuery<AzureTablesCommit>(CommitsTableName).AsTableServiceQuery(context);
        }
    }
}
