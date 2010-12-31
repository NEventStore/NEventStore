namespace EventStore.Persistence.MongoPersistence
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Norm.BSON;
	using Serialization;

	public static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static MongoCommit ToMongoCommit(this CommitAttempt attempt, ISerialize serializer)
		{
			return attempt.ToCommit().ToMongoCommit(serializer);
		}
		public static MongoCommit ToMongoCommit(this Commit commit, ISerialize serializer)
		{
			return new MongoCommit
			{
				StreamId = commit.StreamId,
				CommitId = commit.CommitId,
				StreamRevision = commit.StreamRevision,
				CommitSequence = commit.CommitSequence,
				Headers = (Dictionary<string, object>)commit.Headers,
				Events = commit.Events.ToList(),
				Snapshot = commit.Snapshot != null ? serializer.Serialize(commit.Snapshot) : null
			};
		}
		public static Commit ToCommit(this MongoCommit mongoCommit, ISerialize serializer)
		{
			return new Commit(
				mongoCommit.StreamId,
				mongoCommit.CommitId,
				mongoCommit.StreamRevision,
				mongoCommit.CommitSequence,
				mongoCommit.Headers,
				mongoCommit.Events,
				mongoCommit.Snapshot != null ? serializer.Deserialize(mongoCommit.Snapshot) : null);
		}

		public static Expando ToMongoExpando(this MongoCommit commit)
		{
			var expando = new Expando();
			expando["_id"] = commit.Id;
			return expando;
		}
		public static Expando ToMongoExpando(this StreamHead stream)
		{
			var expando = new Expando();
			expando["_id"] = stream.StreamId;
			return expando;
		}
	}
}