using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Globalization;
using System.Linq.Expressions;
using EventStore.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence
{
    [DataServiceEntity]
    public class AzureTablesCommit : TableEntity
    {
        // StreamId = PartitionKey
        public int StreamRevision { get; set; }

        public Guid CommitId { get; set; }
        // CommitSequence = RowKey
        public DateTime CommitStamp { get; set; }

        public byte[] Headers { get; set; }

        public byte[] Payload { get; set; }

        public bool Dispatched { get; set; }
    }

    public static class AzureTablesExtensions
    {
        public static AzureTablesCommit ToAzureTablesCommit(this Commit commit, ISerialize serializer)
        {
            var headers = serializer.Serialize(commit.Headers);
            var payload = serializer.Serialize(commit.Events);

            if (headers.Length > (64 * 1000) || payload.Length >= (64 * 1000))
            {
                // Handle this more gracefully, but note table entities are limited to 1 MB.
                throw new InvalidOperationException("Events / Headers too big to serialize (>= 64k).");
            }

            return new AzureTablesCommit()
            {
                PartitionKey = GetPartitionKey(commit),
                StreamRevision = commit.StreamRevision,
                CommitId = commit.CommitId,
                RowKey = GetRowKey(commit),
                CommitStamp = commit.CommitStamp,
                Headers = headers,
                Payload = payload,
                Dispatched = false,
            };
        }

        public static Commit ToCommit(this AzureTablesCommit commit, ISerialize serializer)
        {
            var headers = serializer.Deserialize<Dictionary<string, object>>(commit.Headers);
            var payload = serializer.Deserialize<List<EventMessage>>(commit.Payload);
            var streamId = GetStreamId(commit);
            var commitSequence = GetCommitSequence(commit);

            return new Commit(streamId, commit.StreamRevision, commit.CommitId, commitSequence, commit.CommitStamp,
                              headers, payload);
        }

        public static TableOperation ToPointQuery(this Commit commit)
        {
            return TableOperation.Retrieve<AzureTablesCommit>(GetPartitionKey(commit), GetRowKey(commit));
        }

        private static string GetPartitionKey(Commit commit)
        {
            return commit.StreamId.ToString();
        }

        private static string GetRowKey(Commit commit)
        {
            return commit.CommitSequence.ToString(CultureInfo.InvariantCulture);
        }

        private static Guid GetStreamId(AzureTablesCommit commit)
        {
            return Guid.Parse(commit.PartitionKey);
        }

        private static int GetCommitSequence(AzureTablesCommit commit)
        {
            return int.Parse(commit.RowKey, CultureInfo.InvariantCulture);
        }
    }
}