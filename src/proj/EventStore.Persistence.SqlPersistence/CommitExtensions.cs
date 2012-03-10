namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using Logging;
	using Serialization;

	public static class CommitExtensions
	{
		private const int StreamIdIndex = 0;
		private const int StreamRevisionIndex = 1;
		private const int CommitIdIndex = 2;
		private const int CommitSequenceIndex = 3;
		private const int CommitStampIndex = 4;
		private const int HeadersIndex = 5;
		private const int PayloadIndex = 6;
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(CommitExtensions));

		public static Commit GetCommit(this IDataRecord record, ISerialize serializer)
		{
			Logger.Verbose(Messages.DeserializingCommit, serializer.GetType());
			var headers = serializer.Deserialize<Dictionary<string, object>>(record, HeadersIndex);
			var events = serializer.Deserialize<List<EventMessage>>(record, PayloadIndex);

			return new Commit(
				record[StreamIdIndex].ToGuid(),
				record[StreamRevisionIndex].ToInt(),
				record[CommitIdIndex].ToGuid(),
				record[CommitSequenceIndex].ToInt(),
				record[CommitStampIndex].ToDateTime(),
				headers,
				events);
		}

		public static Guid StreamId(this IDataRecord record)
		{
			return record[StreamIdIndex].ToGuid();
		}
		public static int CommitSequence(this IDataRecord record)
		{
			return record[CommitSequenceIndex].ToInt();
		}

		public static T Deserialize<T>(this ISerialize serializer, IDataRecord record, int index)
		{
			if (index >= record.FieldCount)
				return default(T);

			var value = record[index];
			if (value == null || value == DBNull.Value)
				return default(T);

			var bytes = (byte[])value;
			return bytes.Length == 0 ? default(T) : serializer.Deserialize<T>(bytes);
		}
	}
}