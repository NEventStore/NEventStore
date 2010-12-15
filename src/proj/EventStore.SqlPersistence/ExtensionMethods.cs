namespace EventStore.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.IO;
	using System.Linq;
	using Serialization;

	public static class ExtensionMethods
	{
		private const int StreamIdIndex = 0;
		private const int CommitIdIndex = 1;
		private const int StreamRevisionIndex = 2;
		private const int CommitSequenceIndex = 3;
		private const int PayloadIndex = 4;
		private const int SnapshotIndex = 5;

		public static IDataParameter AddParameter(this IDbCommand command, string parameterName, object value)
		{
			if (value is Guid)
				value = ((Guid)value).ToByteArray();

			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value ?? DBNull.Value;

			if (parameter.Value == DBNull.Value || value is byte[])
				parameter.DbType = DbType.Binary;

			command.Parameters.Add(parameter);
			return parameter;
		}

		public static byte[] Serialize(this ISerialize serializer, object value)
		{
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, value);
				return stream.ToArray();
			}
		}
		public static object Deserialize(this ISerialize serializer, byte[] serialized)
		{
			serialized = serialized ?? new byte[] { };
			if (serialized.Length == 0)
				return null;

			using (var stream = new MemoryStream(serialized))
				return serializer.Deserialize(stream);
		}

		public static IEnumerable<T> ExecuteQuery<T>(this IDbCommand query, Func<IDataRecord, T> selector)
		{
			return query.ExecuteReader().AsEnumerable().Select(selector).ToArray();
		}
		private static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
		{
			using (reader)
				while (reader.Read())
					yield return reader;
		}

		public static void ExecuteAndSuppressExceptions(this IDbCommand command)
		{
			try
			{
				command.ExecuteNonQuery();
			}
			catch (DbException)
			{
			}
		}

		public static Commit GetCommit(this IDataRecord record, ISerialize serializer)
		{
			var serializedCommit = (byte[])record[PayloadIndex];
			var commit = (Commit)serializer.Deserialize(serializedCommit);

			var serializedSnapshot = record[SnapshotIndex].GetBytes();
			var snapshot = serializer.Deserialize(serializedSnapshot);

			return new Commit(
				(Guid)record[StreamIdIndex],
				(Guid)record[CommitIdIndex],
				(long)record[StreamRevisionIndex],
				(long)record[CommitSequenceIndex],
				commit.Headers,
				commit.Events,
				snapshot);
		}
		private static byte[] GetBytes(this object value)
		{
			return value == null || value == DBNull.Value ? null : (byte[])value;
		}
	}
}