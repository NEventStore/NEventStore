namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using MongoDB.Bson;
	using MongoDB.Driver.Builders;
	using Serialization;

	internal static class ExtensionMethods
	{
		public static MongoCommit ToMongoCommit(this Commit commit, ISerialize serializer)
		{
			var payload = null == serializer ? null : serializer.Serialize(commit.Events);

			return new MongoCommit
			{
				Id = new MongoCommitId(commit.StreamId, commit.CommitSequence),
				StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
				StreamRevision = commit.StreamRevision,
				CommitId = commit.CommitId,
				CommitStamp = commit.CommitStamp,
				Headers = commit.Headers,
				Payload = payload
			};
		}
		public static Commit ToCommit(this MongoCommit commit, ISerialize serializer)
		{
			return new Commit(
				commit.Id.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.Id.CommitSequence,
				commit.CommitStamp,
				commit.Headers,
				serializer.Deserialize(commit.Payload) as List<EventMessage>);
		}

		public static MongoSnapshot ToMongoSnapshot(this Snapshot snapshot, ISerialize serializer)
		{
			return new MongoSnapshot
			{
				Id = new MongoSnapshotId(snapshot.StreamId, snapshot.StreamRevision),
				Payload = serializer.Serialize(snapshot.Payload)
			};
		}
		public static Snapshot ToSnapshot(this MongoSnapshot snapshot, ISerialize serializer)
		{
			if (snapshot == null)
				return null;

			return new Snapshot(
				snapshot.Id.StreamId,
				snapshot.Id.StreamRevision,
				serializer.Deserialize(snapshot.Payload));
		}

		public static StreamHead ToStreamHead(this MongoStreamHead streamhead)
		{
			return new StreamHead(
				streamhead.StreamId,
				streamhead.HeadRevision,
				streamhead.SnapshotRevision);
		}

		public static QueryComplete ToMongoCommitIdQuery(this Commit commit)
		{
			return commit.ToMongoCommit(null).ToMongoCommitIdQuery();
		}
		public static QueryComplete ToMongoCommitIdQuery(this MongoCommit commit)
		{
			return Query.EQ("_id", Query.And(Query.EQ("StreamId", commit.Id.StreamId), Query.EQ("CommitSequence", commit.Id.CommitSequence)).ToBsonDocument());
		}

		public static QueryConditionList ToSnapshotQuery(this Guid streamId, int maxRevision)
		{
			return Query.GT("_id", Query.And(Query.EQ("StreamId", streamId), Query.EQ("StreamRevision", BsonNull.Value)).ToBsonDocument()).LTE(Query.And(Query.EQ("StreamId", streamId), Query.EQ("StreamRevision", maxRevision)).ToBsonDocument());
		}
	}
}