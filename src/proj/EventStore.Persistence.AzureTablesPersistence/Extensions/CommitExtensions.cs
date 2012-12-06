using System;
using System.Collections.Generic;
using System.Globalization;
using EventStore.Persistence.AzureTablesPersistence.Datastructures;
using EventStore.Serialization;

namespace EventStore.Persistence.AzureTablesPersistence.Extensions
{
    public static class CommitExtensions
    {
         public static string GetPartitionKey(this Commit commit)
         {
             return commit.StreamId.ToString();
         }

        public static string GetRowKey(this Commit commit)
        {
            return commit.CommitSequence.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static class AzureTablesCommitExtensions
    {
        public static AzureCommit ToAzureTablesCommit(this Commit commit, ISerialize serializer)
        {
            var headers = serializer.Serialize(commit.Headers);
            var payload = serializer.Serialize(commit.Events);

            if (headers.Length >= (64 * 1000) || payload.Length >= (64 * 1000))
            {
                //TODO: Handle this more gracefully, but note that table entities are limited to 1 MB.
                throw new InvalidOperationException("Events / Headers too big to serialize (>= 64k).");
            }

            return new AzureCommit()
            {
                PartitionKey = commit.GetPartitionKey(),
                StreamRevision = commit.StreamRevision,
                CommitId = commit.CommitId,
                RowKey = commit.GetRowKey(),
                CommitStamp = commit.CommitStamp,
                Headers = headers,
                Payload = payload,
                Dispatched = false,
            };
        }

        public static Commit ToCommit(this AzureCommit commit, ISerialize serializer)
        {
            var headers = serializer.Deserialize<Dictionary<string, object>>(commit.Headers);
            var payload = serializer.Deserialize<List<EventMessage>>(commit.Payload);
            var streamId = commit.GetStreamId();
            var commitSequence = commit.GetCommitSequence();

            return new Commit(streamId, commit.StreamRevision, commit.CommitId, commitSequence, commit.CommitStamp,
                              headers, payload);
        }

        public static Guid GetStreamId(this AzureCommit commit)
        {
            return Guid.Parse(commit.PartitionKey);
        }

        public static int GetCommitSequence(this AzureCommit commit)
        {
            return int.Parse(commit.RowKey, CultureInfo.InvariantCulture);
        }
    }
}