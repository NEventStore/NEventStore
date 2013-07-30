namespace NEventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MongoDB.Bson;
    using MongoDB.Bson.IO;
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Options;
    using MongoDB.Bson.Serialization.Serializers;
    using MongoDB.Driver;
    using MongoDB.Driver.Builders;
    using NEventStore.Serialization;

    public static class ExtensionMethods
    {
        public static Dictionary<Tkey, Tvalue> AsDictionary<Tkey, Tvalue>(this BsonValue bsonValue)
        {
            using (BsonReader reader = BsonReader.Create(bsonValue.ToJson()))
            {
                var dictionarySerializer = new DictionarySerializer<Tkey, Tvalue>();
                object result = dictionarySerializer.Deserialize(reader,
                    typeof (Dictionary<Tkey, Tvalue>),
                    new DictionarySerializationOptions());
                return (Dictionary<Tkey, Tvalue>) result;
            }
        }

        public static BsonDocument ToMongoCommit(this Commit commit, IDocumentSerializer serializer)
        {
            int streamRevision = commit.StreamRevision - (commit.Events.Count - 1);
            IEnumerable<BsonDocument> events =
                commit.Events.Select(
                                     e =>
                                         new BsonDocument
                                         {
                                             {"StreamRevision", streamRevision++},
                                             {"Payload", new BsonDocumentWrapper(typeof (EventMessage), serializer.Serialize(e))}
                                         });
            return new BsonDocument
            {
                {"_id", new BsonDocument {{"StreamId", commit.StreamId}, {"CommitSequence", commit.CommitSequence}}},
                {"CommitId", commit.CommitId},
                {"CommitStamp", commit.CommitStamp},
                {"Headers", BsonDocumentWrapper.Create(commit.Headers)},
                {"Events", new BsonArray(events)},
                {"Dispatched", false}
            };
        }

        public static Commit ToCommit(this BsonDocument doc, IDocumentSerializer serializer)
        {
            if (doc == null)
            {
                return null;
            }

            BsonDocument id = doc["_id"].AsBsonDocument;
            Guid streamId = id["StreamId"].AsGuid;
            int commitSequence = id["CommitSequence"].AsInt32;

            List<EventMessage> events =
                doc["Events"].AsBsonArray.Select(
                                                 e =>
                                                     e.AsBsonDocument["Payload"].IsBsonDocument
                                                         ? BsonSerializer.Deserialize<EventMessage>(
                                                                                                    e.AsBsonDocument["Payload"]
                                                                                                        .AsBsonDocument)
                                                         : serializer.Deserialize<EventMessage>(e.AsBsonDocument["Payload"].AsByteArray))
                    .ToList();
            int streamRevision = doc["Events"].AsBsonArray.Last().AsBsonDocument["StreamRevision"].AsInt32;
            return new Commit(streamId,
                streamRevision,
                doc["CommitId"].AsGuid,
                commitSequence,
                doc["CommitStamp"].ToUniversalTime(),
                doc["Headers"].AsDictionary<string, object>(),
                events);
        }

        public static BsonDocument ToMongoSnapshot(this Snapshot snapshot, IDocumentSerializer serializer)
        {
            return new BsonDocument
            {
                {"_id", new BsonDocument {{"StreamId", snapshot.StreamId}, {"StreamRevision", snapshot.StreamRevision}}},
                {"Payload", BsonDocumentWrapper.Create(serializer.Serialize(snapshot.Payload))}
            };
        }

        public static Snapshot ToSnapshot(this BsonDocument doc, IDocumentSerializer serializer)
        {
            if (doc == null)
            {
                return null;
            }

            BsonDocument id = doc["_id"].AsBsonDocument;
            Guid streamId = id["StreamId"].AsGuid;
            int streamRevision = id["StreamRevision"].AsInt32;
            BsonValue bsonPayload = doc["Payload"];

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
                    payload = BsonTypeMapper.MapToDotNetValue(bsonPayload);
                    break;
            }

            return new Snapshot(streamId, streamRevision, payload);
        }

        public static StreamHead ToStreamHead(this BsonDocument doc)
        {
            return new StreamHead(doc["_id"].AsGuid, doc["HeadRevision"].AsInt32, doc["SnapshotRevision"].AsInt32);
        }

        public static IMongoQuery ToMongoCommitIdQuery(this Commit commit)
        {
            return Query.EQ("_id",
                Query.And(Query.EQ("StreamId", commit.StreamId), Query.EQ("CommitSequence", commit.CommitSequence)).ToBsonDocument());
        }

        public static IMongoQuery ToSnapshotQuery(this Guid streamId, int maxRevision)
        {
            return
                Query.And(
                          Query.GT("_id",
                              Query.And(Query.EQ("StreamId", streamId), Query.EQ("StreamRevision", BsonNull.Value)).ToBsonDocument()),
                    Query.LTE("_id", Query.And(Query.EQ("StreamId", streamId), Query.EQ("StreamRevision", maxRevision)).ToBsonDocument()));
        }
    }
}