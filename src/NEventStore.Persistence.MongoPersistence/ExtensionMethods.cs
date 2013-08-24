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
    using BsonSerializer = MongoDB.Bson.Serialization.BsonSerializer;

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
            IEnumerable<BsonDocument> events = commit
                .Events
                .Select(e =>
                    new BsonDocument
                    {
                        {"StreamRevision", streamRevision++},
                        {"Payload", new BsonDocumentWrapper(typeof (EventMessage), serializer.Serialize(e))}
                    });
            return new BsonDocument
            {
                {MongoFields.Id, new BsonDocument
                    {
                        {MongoFields.BucketId, commit.BucketId},
                        {MongoFields.StreamId, commit.StreamId},
                        {MongoFields.CommitSequence, commit.CommitSequence}
                    }
                 },
                {MongoFields.CommitId, commit.CommitId},
                {MongoFields.CommitStamp, commit.CommitStamp},
                {MongoFields.Headers, BsonDocumentWrapper.Create(commit.Headers)},
                {MongoFields.Events, new BsonArray(events)},
                {MongoFields.Dispatched, false}
            };
        }

        public static Commit ToCommit(this BsonDocument doc, IDocumentSerializer serializer)
        {
            if (doc == null)
            {
                return null;
            }

            BsonDocument id = doc[MongoFields.Id].AsBsonDocument;
            string bucketId = id[MongoFields.BucketId].AsString;
            string streamId = id[MongoFields.StreamId].AsString;
            int commitSequence = id[MongoFields.CommitSequence].AsInt32;

            List<EventMessage> events = doc[MongoFields.Events]
                .AsBsonArray
                .Select(e =>
                    e.AsBsonDocument[MongoFields.Payload].IsBsonDocument
                        ? BsonSerializer.Deserialize<EventMessage>(e.AsBsonDocument[MongoFields.Payload].AsBsonDocument)
                        : serializer.Deserialize<EventMessage>(e.AsBsonDocument[MongoFields.Payload].AsByteArray))
                .ToList();
            int streamRevision = doc[MongoFields.Events].AsBsonArray.Last().AsBsonDocument[MongoFields.StreamRevision].AsInt32;
            return new Commit(
                bucketId,
                streamId,
                streamRevision,
                doc[MongoFields.CommitId].AsGuid,
                commitSequence,
                doc[MongoFields.CommitStamp].ToUniversalTime(),
                doc[MongoFields.Headers].AsDictionary<string, object>(),
                events);
        }

        public static BsonDocument ToMongoSnapshot(this Snapshot snapshot, IDocumentSerializer serializer)
        {
            return new BsonDocument
            {
                { MongoFields.Id, new BsonDocument
                    {
                        {MongoFields.BucketId, snapshot.BucketId},
                        {MongoFields.StreamId, snapshot.StreamId},
                        {MongoFields.StreamRevision, snapshot.StreamRevision}
                    }
                },
                { MongoFields.Payload, BsonDocumentWrapper.Create(serializer.Serialize(snapshot.Payload)) }
            };
        }

        public static Snapshot ToSnapshot(this BsonDocument doc, IDocumentSerializer serializer)
        {
            if (doc == null)
            {
                return null;
            }

            BsonDocument id = doc[MongoFields.Id].AsBsonDocument;
            string bucketId = id[MongoFields.BucketId].AsString;
            string streamId = id[MongoFields.StreamId].AsString;
            int streamRevision = id[MongoFields.StreamRevision].AsInt32;
            BsonValue bsonPayload = doc[MongoFields.Payload];

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

            return new Snapshot(bucketId, streamId, streamRevision, payload);
        }

        public static StreamHead ToStreamHead(this BsonDocument doc)
        {
            BsonDocument id = doc[MongoFields.Id].AsBsonDocument;
            string bucketId = id[MongoFields.BucketId].AsString;
            string streamId = id[MongoFields.StreamId].AsString;
            return new StreamHead(bucketId, streamId, doc[MongoFields.HeadRevision].AsInt32, doc[MongoFields.SnapshotRevision].AsInt32);
        }

        public static IMongoQuery ToMongoCommitIdQuery(this Commit commit)
        {
            return Query.EQ(MongoFields.Id, Query.And(
                    Query.EQ(MongoFields.BucketId, commit.BucketId),
                    Query.EQ(MongoFields.StreamId, commit.StreamId),
                    Query.EQ(MongoFields.CommitSequence, commit.CommitSequence))
                .ToBsonDocument());
        }

        public static IMongoQuery GetSnapshotQuery(string bucketId, string streamId, int maxRevision)
        {
            return
                Query.And(
                    Query.GT(MongoFields.Id,
                        Query.And(
                            Query.EQ(MongoFields.BucketId, bucketId),
                            Query.EQ(MongoFields.StreamId, streamId),
                            Query.EQ(MongoFields.StreamRevision, BsonNull.Value)
                        ).ToBsonDocument()),
                    Query.LTE(MongoFields.Id,
                        Query.And(
                            Query.EQ(MongoFields.BucketId, bucketId),
                            Query.EQ(MongoFields.StreamId, streamId),
                            Query.EQ(MongoFields.StreamRevision, maxRevision)
                         ).ToBsonDocument())
                    );
                }
        }
}