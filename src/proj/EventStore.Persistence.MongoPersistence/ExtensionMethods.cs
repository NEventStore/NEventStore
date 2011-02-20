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
		public static MongoCommit ToMongoCommit(this Commit commit, ISerialize serializer)
		{
			return new MongoCommit
			{
				Id = new MongoCommitId(commit.StreamId, commit.CommitSequence),
				StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
				StreamRevision = commit.StreamRevision,
				CommitId = commit.CommitId,
				CommitStamp = commit.CommitStamp,
				Headers = commit.Headers,
				Payload = commit.Events.ToBsonValue(serializer)
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
				commit.Payload.ToEventList(serializer));
		}

		public static MongoSnapshot ToMongoSnapshot(this Snapshot snapshot, ISerialize serializer)
		{
			return new MongoSnapshot
			{
				Id = new MongoSnapshotId(snapshot.StreamId, snapshot.StreamRevision),
				Payload = snapshot.Payload.ToBsonValue(serializer)
			};
		}
		public static Snapshot ToSnapshot(this BsonDocument bsonDocument, ISerialize serializer)
		{
			if (bsonDocument == null)
				return null;

			var id = BsonSerializer.Deserialize<MongoSnapshotId>(bsonDocument["_id"].AsBsonDocument);
			var bsonPayload = bsonDocument["Payload"];
			var payload = bsonPayload.IsBsonDocument
				? BsonSerializer.Deserialize(bsonPayload.AsBsonDocument, typeof(object))
				: bsonPayload.RawValue;

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

		public static BsonValue ToBsonValue(this object payload, ISerialize serializer)
		{
			if (serializer == null)
				return BsonNull.Value;

			if (!(serializer is MongoSerializer))
				return BsonBinaryData.Create(serializer.Serialize(payload));

			BsonValue result;
			try
			{
				// we'll normally expect the snapshot to be an object that has a ClassMap
				// but this will fail if it's a simple value type instead (or a string)
				result = payload.ToBsonDocument();
			}
			catch (InvalidOperationException)
			{
				// ... so we have to use this instead (this is only likely in the unit tests)
				result = BsonValue.Create(payload);
			}
			return result;
		}
		public static BsonValue ToBsonValue(this List<EventMessage> events, ISerialize serializer)
		{
			if (serializer == null)
				return BsonNull.Value;

			if (!(serializer is MongoSerializer))
				return BsonBinaryData.Create(serializer.Serialize(events));

			var result = new BsonArray();
			foreach (var e in events)
			{
				result.Add(e.ToBsonDocument());
			}
			return result;
		}
		public static List<EventMessage> ToEventList(this BsonValue value, ISerialize serializer)
		{
			if (serializer == null)
				return new List<EventMessage>();

			if (!(serializer is MongoSerializer))
				return serializer.Deserialize<List<EventMessage>>(value.AsByteArray);

			return value.AsBsonArray.Select(item => BsonSerializer.Deserialize<EventMessage>(item.AsBsonDocument)).ToList();
		}
	}
}