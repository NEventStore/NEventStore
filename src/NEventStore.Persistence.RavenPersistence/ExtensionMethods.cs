namespace NEventStore.Persistence.RavenPersistence
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NEventStore.Serialization;

    public static class ExtensionMethods
    {
        public static string ToRavenCommitId(this CommitAttempt commit)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", commit.BucketId, commit.StreamId, commit.CommitSequence);
        }

        public static string ToRavenCommitId(this ICommit commit)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", commit.BucketId, commit.StreamId, commit.CommitSequence);
        }

        public static RavenCommit ToRavenCommit(this CommitAttempt commit, IDocumentSerializer serializer)
        {
            return new RavenCommit
            {
                Id = ToRavenCommitId(commit),
                BucketId = commit.BucketId,
                StreamId = commit.StreamId,
                CommitSequence = commit.CommitSequence,
                StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
                StreamRevision = commit.StreamRevision,
                CommitId = commit.CommitId,
                CommitStamp = commit.CommitStamp,
                Headers = commit.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Payload = serializer.Serialize(commit.Events)
            };
        }

        public static ICommit ToCommit(this RavenCommit commit, IDocumentSerializer serializer)
        {
            return new Commit(
                commit.BucketId,
                commit.StreamId,
                commit.StreamRevision,
                commit.CommitId,
                commit.CommitSequence,
                commit.CommitStamp,
                null,
                commit.Headers,
                serializer.Deserialize<List<IEventMessage>>(commit.Payload));
        }

        public static string ToRavenSnapshotId(ISnapshot snapshot)
        {
            return string.Format("Snapshots/{0}/{1}/{2}", snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision);
        }

        public static RavenSnapshot ToRavenSnapshot(this ISnapshot snapshot, IDocumentSerializer serializer)
        {
            return new RavenSnapshot
            {
                Id = ToRavenSnapshotId(snapshot),
                BucketId = snapshot.BucketId,
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

            return new Snapshot(snapshot.BucketId, snapshot.StreamRevision, serializer.Deserialize<object>(snapshot.Payload));
        }

        public static RavenStreamHead ToRavenStreamHead(this CommitAttempt commit)
        {
            return new RavenStreamHead
            {
                Id = RavenStreamHead.GetStreamHeadId(commit.BucketId, commit.StreamId),
                BucketId = commit.BucketId,
                StreamId = commit.StreamId,
                HeadRevision = commit.StreamRevision,
                SnapshotRevision = 0
            };
        }

        public static RavenStreamHead ToRavenStreamHead(this ISnapshot snapshot)
        {
            return new RavenStreamHead
            {
                Id = RavenStreamHead.GetStreamHeadId(snapshot.BucketId, snapshot.StreamId),
                BucketId = snapshot.BucketId,
                StreamId = snapshot.StreamId,
                HeadRevision = snapshot.StreamRevision,
                SnapshotRevision = snapshot.StreamRevision
            };
        }

        public static StreamHead ToStreamHead(this RavenStreamHead streamHead)
        {
            return new StreamHead(streamHead.BucketId, streamHead.StreamId, streamHead.HeadRevision, streamHead.SnapshotRevision);
        }
    }
}