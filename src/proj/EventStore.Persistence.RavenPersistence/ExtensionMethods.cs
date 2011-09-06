namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Serialization;

	public static class ExtensionMethods
	{
		public static string ToRavenCommitId(this Commit commit)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", commit.StreamId, commit.CommitSequence);
		}

		public static RavenCommit ToRavenCommit(this Commit commit, IDocumentSerializer serializer)
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

		public static Commit ToCommit(this RavenCommit commit, IDocumentSerializer serializer)
		{
			return new Commit(
				commit.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.CommitSequence,
				commit.CommitStamp,
				commit.Headers,
				serializer.Deserialize<List<EventMessage>>(commit.Payload.FromBase64()));
		}
		private static object FromBase64(this object value)
		{
			const string Base64Prefix = "AAEAAAD/////";

			if (!(value is string))
				return value;

			var @string = value as string;
			if (string.IsNullOrEmpty(@string))
				return value;

			if (@string.StartsWith(Base64Prefix))
				return Convert.FromBase64String(value as string);

			return value;
		}

		public static RavenSnapshot ToRavenSnapshot(this Snapshot snapshot, IDocumentSerializer serializer)
		{
			return new RavenSnapshot
			{
				StreamId = snapshot.StreamId,
				StreamRevision = snapshot.StreamRevision,
				Payload = serializer.Serialize(snapshot.Payload)
			};
		}

		public static Snapshot ToSnapshot(this RavenSnapshot snapshot, IDocumentSerializer serializer)
		{
			if (snapshot == null)
				return null;

			return new Snapshot(
				snapshot.StreamId,
				snapshot.StreamRevision,
				serializer.Deserialize<object>(snapshot.Payload.FromBase64()));
		}

		public static string ToRavenStreamId(this Guid streamId)
		{
			return string.Format("StreamHeads/{0}", streamId);
		}

		public static RavenStreamHead ToRavenStreamHead(this Commit commit)
		{
			return new RavenStreamHead
			{
				Id = commit.StreamId.ToRavenStreamId(),
				StreamId = commit.StreamId,
				HeadRevision = commit.StreamRevision,
				SnapshotRevision = 0
			};
		}

		public static RavenStreamHead ToRavenStreamHead(this Snapshot snapshot)
		{
			return new RavenStreamHead
			{
				Id = snapshot.StreamId.ToRavenStreamId(),
				StreamId = snapshot.StreamId,
				HeadRevision = snapshot.StreamRevision,
				SnapshotRevision = snapshot.StreamRevision
			};
		}

		public static StreamHead ToStreamHead(this RavenStreamHead streamhead)
		{
			return new StreamHead(
				streamhead.StreamId,
				streamhead.HeadRevision,
				streamhead.SnapshotRevision);
		}

		public static IEnumerable<T> Page<T>(this IQueryable<T> query, int pageSize)
		{
			return new PagedEnumeration<T>(query, pageSize);
		}
	}
}