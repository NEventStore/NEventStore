namespace EventStore.Persistence.MongoPersistence
{
	using System.Globalization;
	using Norm.BSON;
	using Serialization;

	internal static class ExtensionMethods
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
				Headers = commit.Headers,
				Events = commit.Events,
				Snapshot = commit.Snapshot != null ? serializer.Serialize(commit.Snapshot) : null
			};
		}
		public static Commit ToCommit(this MongoCommit mongoCommit, ISerialize serializer)
		{
			return new Commit(
				mongoCommit.StreamId,
				mongoCommit.StreamRevision,
				mongoCommit.CommitId,
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