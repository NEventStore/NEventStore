namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Norm.BSON;
	using Serialization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static MongoCommit ToMongoCommit(this Commit commit, ISerialize serializer)
		{
			return new MongoCommit
			{
				StreamId = commit.StreamId,
				StartingStreamRevision = commit.StreamRevision - (commit.Events.Count - 1),
				StreamRevision = commit.StreamRevision,
				CommitId = commit.CommitId,
				CommitSequence = commit.CommitSequence,
				CommitStamp = DateTime.Now,
				Headers = commit.Headers,
				Payload = serializer.Serialize(commit.Events)
			};
		}
		public static Commit ToCommit(this MongoCommit commit, ISerialize serializer)
		{
			return new Commit(
				commit.StreamId,
				commit.StreamRevision,
				commit.CommitId,
				commit.CommitSequence,
				commit.Headers,
				serializer.Deserialize(commit.Payload) as List<EventMessage>);
		}

		public static MongoSnapshot ToMongoSnapshot(this Snapshot snapshot, ISerialize serializer)
		{
			return new MongoSnapshot
			{
				StreamId = snapshot.StreamId,
				StreamRevision = snapshot.StreamRevision,
				Payload = serializer.Serialize(snapshot.Payload)
			};
		}
		public static Snapshot ToSnapshot(this MongoSnapshot snapshot, ISerialize serializer)
		{
			if (snapshot == null)
				return null;

			return new Snapshot(
				snapshot.StreamId,
				snapshot.StreamRevision,
				serializer.Deserialize(snapshot.Payload));
		}

		public static Expando ToMongoExpando(this MongoCommit commit)
		{
			var expando = new Expando();
			expando["_id"] = commit.Id;
			return expando;
		}
	}
}