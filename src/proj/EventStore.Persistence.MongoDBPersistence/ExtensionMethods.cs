namespace EventStore.Persistence.MongoDBPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using MongoDB.Driver.Builders;
	using Serialization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static MongoDBCommit ToMongoDBCommit(this Commit commit, ISerialize serializer)
		{
			return new MongoDBCommit
			{
				Id = new MongoDBCommitId(commit.StreamId, commit.CommitSequence),
				StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
				StreamRevision = commit.StreamRevision,
				CommitId = commit.CommitId,
				CommitStamp = DateTime.Now,
				Headers = commit.Headers,
				Payload = serializer.Serialize(commit.Events)
			};
		}

		public static Commit ToCommit(this MongoDBCommit commit, ISerialize serializer)
		{
			return new Commit(
				commit.Id.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.Id.CommitSequence,
				commit.Headers,
				serializer.Deserialize(commit.Payload) as List<EventMessage>);
		}

		public static MongoDBSnapshot ToMongoDBSnapshot(this Snapshot snapshot, ISerialize serializer)
		{
			return new MongoDBSnapshot
			{
				Id = new MongoDBSnapshotId(snapshot.StreamId, snapshot.StreamRevision),
				Payload = serializer.Serialize(snapshot.Payload)
			};
		}

		public static Snapshot ToSnapshot(this MongoDBSnapshot snapshot, ISerialize serializer)
		{
			if (snapshot == null)
				return null;

			return new Snapshot(
				snapshot.Id.StreamId,
				snapshot.Id.StreamRevision,
				serializer.Deserialize(snapshot.Payload));
		}

		public static StreamHead ToStreamHead(this MongoDBStreamHead streamhead)
		{
			return new StreamHead(
				streamhead.StreamId,
				streamhead.HeadRevision,
				streamhead.SnapshotRevision);
		}

		public static QueryComplete ToMongoDBCommitIdQuery(this Commit commit)
		{
			var query = Query.EQ("_id",
				Query.And(
					Query.EQ("StreamId", commit.StreamId),
					Query.EQ("CommitSequence", commit.CommitSequence)).ToBsonDocument());

			return query;
		}

		public static QueryComplete ToMongoDBCommitIdQuery(this MongoDBCommit commit)
		{
			var query = Query.EQ("_id",
				Query.And(
					Query.EQ("StreamId", commit.Id.StreamId),
					Query.EQ("CommitSequence", commit.Id.CommitSequence)
					).ToBsonDocument()
			);

			return query;
		}
	}
}