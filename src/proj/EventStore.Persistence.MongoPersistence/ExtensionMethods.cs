namespace EventStore.Persistence.MongoPersistence
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Norm.BSON;

    public static class ExtensionMethods
    {

        public static string FormatWith(this string format, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, format, values);
        }

        public static MongoCommit ToMongoCommit(this CommitAttempt attempt)
        {
            return attempt.ToCommit().ToMongoCommit();
        }
        public static MongoCommit ToMongoCommit(this Commit commit)
        {
            return new MongoCommit
            {
                StreamId = commit.StreamId,
                CommitId = commit.CommitId,
                StreamRevision = commit.StreamRevision,
                CommitSequence = commit.CommitSequence,
                Headers = (Dictionary<string, object>)commit.Headers,
                Events = commit.Events.ToList(),
                Snapshot = commit.Snapshot
            };
        }
        public static Commit ToCommit(this MongoCommit commit)
        {
            return new Commit(
                commit.StreamId,
                commit.CommitId,
                commit.StreamRevision,
                commit.CommitSequence,
                commit.Headers,
                commit.Events,
                commit.Snapshot);
        }

        public static Expando ToMongoQuery(this MongoCommit commit)
        {
            var expando = new Expando();

            expando["_id"] = commit.Id;

            return expando;
        }

        public static Expando ToMongoExpando(this Stream stream)
        {
            var expando = new Expando();

            expando["_id"] = stream.StreamId;

            return expando;
        }

    }
}