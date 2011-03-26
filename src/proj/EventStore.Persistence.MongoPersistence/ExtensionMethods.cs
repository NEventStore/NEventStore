namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson;
	using MongoDB.Bson.Serialization;
	using MongoDB.Driver.Builders;
	using Serialization;

	public static class ExtensionMethods
	{
		public static MongoCommit ToMongoCommit(this Commit commit, IDocumentSerializer serializer)
		{
			return new MongoCommit
			{
				Id = new MongoCommitId(commit.StreamId, commit.CommitSequence),
				StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
				StreamRevision = commit.StreamRevision,
				CommitId = commit.CommitId,
				CommitStamp = commit.CommitStamp,
				Headers = commit.Headers,
				Payload = BsonDocumentWrapper.Create(serializer.Serialize(commit.Events))
			};
		}
		public static Commit ToCommit(this MongoCommit commit, IDocumentSerializer serializer)
		{
			return new Commit(
				commit.Id.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.Id.CommitSequence,
				commit.CommitStamp,
				commit.Headers,
				commit.Payload.IsBsonBinaryData ? serializer.Deserialize<List<EventMessage>>(commit.Payload.AsByteArray) : commit.Payload.AsBsonArray.Select(item => BsonSerializer.Deserialize<EventMessage>(item.AsBsonDocument)).ToList());
		}

		public static MongoSnapshot ToMongoSnapshot(this Snapshot snapshot, IDocumentSerializer serializer)
		{
			return new MongoSnapshot
			{
				Id = new MongoSnapshotId(snapshot.StreamId, snapshot.StreamRevision),
				Payload = BsonDocumentWrapper.Create(serializer.Serialize(snapshot.Payload))
			};
		}

		public static Snapshot ToSnapshot(this BsonDocument bsonDocument, IDocumentSerializer serializer)
		{
			if (bsonDocument == null)
				return null;

			var id = BsonSerializer.Deserialize<MongoSnapshotId>(bsonDocument["_id"].AsBsonDocument);
			var bsonPayload = bsonDocument["Payload"];

			object payload;
			switch(bsonPayload.BsonType)
			{
				case BsonType.Binary:
					payload = serializer.Deserialize<object>(bsonPayload.AsByteArray);
					break;
				case BsonType.Document:
					payload = BsonSerializer.Deserialize<object>(bsonPayload.AsBsonDocument);
					break;
				default:
					payload = bsonPayload.RawValue;
					break;
			}

			return new Snapshot(
				id.StreamId,
				id.StreamRevision,
				payload);
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
			return Query.EQ("_id", Query.And(Query.EQ("StreamId", commit.StreamId), Query.EQ("CommitSequence", commit.CommitSequence)).ToBsonDocument());
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