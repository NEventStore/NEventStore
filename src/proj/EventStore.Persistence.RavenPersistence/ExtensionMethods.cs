namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Serialization;

	internal static class ExtensionMethods
	{
		public static string ToRavenCommitId(this Commit commit)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", commit.StreamId, commit.CommitSequence);
		}

		public static RavenCommit ToRavenCommit(this Commit commit, ISerialize serializer)
		{
			return new RavenCommit
			{
				Id = ToRavenCommitId(commit),
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

		public static Commit ToCommit(this RavenCommit commit, ISerialize serializer)
		{
			return new Commit(
				commit.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.CommitSequence,
				commit.CommitStamp,
				commit.Headers,
				serializer.Deserialize(commit.Payload) as List<EventMessage>);
		}

		public static RavenSnapshot ToRavenSnapshot(this Snapshot snapshot, ISerialize serializer)
		{
			return new RavenSnapshot
			{
				StreamId = snapshot.StreamId,
				StreamRevision = snapshot.StreamRevision,
				Payload = serializer.Serialize(snapshot.Payload)
			};
		}

		public static Snapshot ToSnapshot(this RavenSnapshot snapshot, ISerialize serializer)
		{
			if (snapshot == null)
				return null;

			return new Snapshot(
				snapshot.StreamId,
				snapshot.StreamRevision,
				serializer.Deserialize(snapshot.Payload));
		}

		public static string ToRavenStreamId(Guid streamId)
		{
			return string.Format("StreamHeads/{0}", streamId);
		}

		public static RavenStreamHead ToRavenStreamHead(this Commit commit)
		{
			return new RavenStreamHead
			{
				Id = ToRavenStreamId(commit.StreamId),
				StreamId = commit.StreamId,
				HeadRevision = commit.StreamRevision,
				SnapshotRevision = 0,
				SnapshotAge = commit.StreamRevision
			};
		}

		public static RavenStreamHead ToRavenStreamHead(this Snapshot snapshot)
		{
			return new RavenStreamHead
			{
				Id = ToRavenStreamId(snapshot.StreamId),
				StreamId = snapshot.StreamId,
				HeadRevision = snapshot.StreamRevision,
				SnapshotRevision = snapshot.StreamRevision,
				SnapshotAge = 0
			};
		}

		public static StreamHead ToStreamHead(this RavenStreamHead streamhead)
		{
			return new StreamHead(
				streamhead.StreamId,
				streamhead.HeadRevision,
				streamhead.SnapshotRevision);
		}
	}
}