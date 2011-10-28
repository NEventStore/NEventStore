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
		public static BsonDocument ToMongoCommit(this Commit commit, IDocumentSerializer serializer)
		{
			var streamRevision = commit.StreamRevision - (commit.Events.Count - 1);
			var events = commit.Events.Select(e => new BsonDocument { { "r", streamRevision++ }, { "p", new BsonDocumentWrapper(typeof(EventMessage), serializer.Serialize(e)) } });
			return new BsonDocument
			{
				{ "_id", commit.CommitId },
				{ "s", commit.CommitStamp },
				{ "i", commit.StreamId },
				{ "n", commit.CommitSequence },
				{ "h", BsonDocumentWrapper.Create(commit.Headers) },
				{ "e", BsonArray.Create(events) },
				{ "q", commit.CommitStamp }
			};
		}
		public static Commit ToCommit(this BsonDocument doc, IDocumentSerializer serializer)
		{
			if (doc == null)
				return null;

			var events = doc["e"].AsBsonArray.Select(e => e.AsBsonDocument["p"].IsBsonDocument ? BsonSerializer.Deserialize<EventMessage>(e.AsBsonDocument["p"].AsBsonDocument) : serializer.Deserialize<EventMessage>(e.AsBsonDocument["p"].AsByteArray)).ToList();
			var streamRevision = doc["e"].AsBsonArray.Last().AsBsonDocument["r"].AsInt32;
			return new Commit(
				doc["i"].AsGuid,
				streamRevision,
				doc["_id"].AsGuid,
				doc["n"].AsInt32,
				doc["s"].AsDateTime,
				BsonSerializer.Deserialize<Dictionary<string, object>>(doc["h"].AsBsonDocument),
				events);
		}

		public static BsonDocument ToMongoSnapshot(this Snapshot snapshot, IDocumentSerializer serializer)
		{
			return new BsonDocument
			{
				{ "_id", new BsonDocument { { "i", snapshot.StreamId }, { "r", snapshot.StreamRevision } } },
				{ "p", BsonDocumentWrapper.Create(serializer.Serialize(snapshot.Payload)) }
			};
		}

		public static Snapshot ToSnapshot(this BsonDocument doc, IDocumentSerializer serializer)
		{
			if (doc == null)
				return null;

			var id = doc["_id"].AsBsonDocument;
			var streamId = id["i"].AsGuid;
			var streamRevision = id["r"].AsInt32;
			var bsonPayload = doc["p"];

			object payload;
			switch (bsonPayload.BsonType)
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
				streamId,
				streamRevision,
				payload);
		}

		public static StreamHead ToStreamHead(this BsonDocument doc)
		{
			return new StreamHead(
				doc["_id"].AsGuid,
				doc["h"].AsInt32,
				doc["s"].AsInt32);
		}

		public static QueryComplete ToMongoCommitIdQuery(this Commit commit)
		{
			return Query.And(Query.EQ("i", commit.StreamId), Query.EQ("n", commit.CommitSequence));
		}

		public static QueryConditionList ToSnapshotQuery(this Guid streamId, int maxRevision)
		{
			return Query.GT("_id", Query.And(Query.EQ("i", streamId), Query.EQ("r", BsonNull.Value)).ToBsonDocument()).LTE(Query.And(Query.EQ("i", streamId), Query.EQ("r", maxRevision)).ToBsonDocument());
		}
	}
}