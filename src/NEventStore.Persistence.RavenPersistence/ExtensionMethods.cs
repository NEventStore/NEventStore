namespace NEventStore.Persistence.RavenPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using NEventStore.Serialization;

    public static class ExtensionMethods
    {
        public static string ToRavenCommitId(this Commit commit, string partition)
        {
            string id = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", commit.StreamId, commit.CommitSequence);

            if (!string.IsNullOrEmpty(partition))
            {
                id = string.Format("{0}/{1}", partition, id);
            }

            return id;
        }

        public static RavenCommit ToRavenCommit(this Commit commit, string partition, IDocumentSerializer serializer)
        {
            return new RavenCommit
            {
                Id = ToRavenCommitId(commit, partition),
                Partition = partition,
                StreamId = commit.StreamId,
                CommitSequence = commit.CommitSequence,
                StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
                StreamRevision = commit.StreamRevision,
                CommitId = commit.CommitId,
                CommitStamp = commit.CommitStamp,
                Headers = commit.Headers,
                Payload = serializer.Serialize(commit.Events)
            };
        }

        public static Commit ToCommit(this RavenCommit commit, IDocumentSerializer serializer)
        {
            return new Commit(commit.StreamId,
                commit.StreamRevision,
                commit.CommitId,
                commit.CommitSequence,
                commit.CommitStamp,
                commit.Headers,
                serializer.Deserialize<List<EventMessage>>(commit.Payload));
        }

        public static string ToRavenSnapshotId(Snapshot snapshot, string partition)
        {
            return string.Format("Snapshots/{0}/{1}", snapshot.StreamId, snapshot.StreamRevision);
        }

        public static RavenSnapshot ToRavenSnapshot(this Snapshot snapshot, string partition, IDocumentSerializer serializer)
        {
            return new RavenSnapshot
            {
                Id = ToRavenSnapshotId(snapshot, partition),
                Partition = partition,
                StreamId = snapshot.StreamId,
                StreamRevision = snapshot.StreamRevision,
                Payload = serializer.Serialize(snapshot.Payload)
            };
        }

        public static Snapshot ToSnapshot(this RavenSnapshot snapshot, IDocumentSerializer serializer)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new Snapshot(snapshot.StreamId, snapshot.StreamRevision, serializer.Deserialize<object>(snapshot.Payload));
        }

        public static string ToRavenStreamId(this string streamId, string partition)
        {
            string id = string.Format("StreamHeads/{0}", streamId);

            if (!string.IsNullOrEmpty(partition))
            {
                id = string.Format("{0}/{1}", partition, id);
            }

            return id;
        }

        public static RavenStreamHead ToRavenStreamHead(this Commit commit, string partition)
        {
            return new RavenStreamHead
            {
                Id = commit.StreamId.ToRavenStreamId(partition),
                Partition = partition,
                StreamId = commit.StreamId,
                HeadRevision = commit.StreamRevision,
                SnapshotRevision = 0
            };
        }

        public static RavenStreamHead ToRavenStreamHead(this Snapshot snapshot, string partition)
        {
            return new RavenStreamHead
            {
                Id = snapshot.StreamId.ToRavenStreamId(partition),
                Partition = partition,
                StreamId = snapshot.StreamId,
                HeadRevision = snapshot.StreamRevision,
                SnapshotRevision = snapshot.StreamRevision
            };
        }

        public static StreamHead ToStreamHead(this RavenStreamHead streamHead)
        {
            throw new NotImplementedException();
            //return new StreamHead(streamHead.StreamId, streamHead.HeadRevision, streamHead.SnapshotRevision);
        }
    }
}