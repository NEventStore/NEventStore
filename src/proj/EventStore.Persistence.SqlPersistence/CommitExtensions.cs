namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using Serialization;

	internal static class CommitExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamRevisionIndex = 1;
		private const int CommitIdIndex = 2;
		private const int CommitSequenceIndex = 3;
		private const int HeadersIndex = 4;
		private const int PayloadIndex = 5;
		private const int SnapshotIndex = 6;

		public static Commit GetCommit(this IDataRecord record, ISerialize serializer)
		{
			var headers = serializer.Deserialize(record, HeadersIndex) as Dictionary<string, object>;
			var events = serializer.Deserialize(record, PayloadIndex) as List<EventMessage>;
			var snapshot = serializer.Deserialize(record, SnapshotIndex);

			return new Commit(
				record[StreamIdIndex].ToGuid(),
				record[StreamRevisionIndex].ToInt(),
				record[CommitIdIndex].ToGuid(),
				record[CommitSequenceIndex].ToInt(),
				headers,
				events,
				snapshot);
		}

		public static object Deserialize(this ISerialize serializer, IDataRecord record, int index)
		{
			if (index >= record.FieldCount)
				return null;

			var value = record[index];
			if (value == null || value == DBNull.Value)
				return null;

			var bytes = (byte[])value;
			return bytes.Length == 0 ? null : serializer.Deserialize(bytes);
		}
	}
}