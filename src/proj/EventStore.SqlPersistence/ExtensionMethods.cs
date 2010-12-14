namespace EventStore.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.IO;
	using System.Linq;
	using Serialization;

	public static class ExtensionMethods
	{
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

		public static long CommitSequence(this CommitAttempt attempt)
		{
			return attempt.PreviousCommitSequence + 1;
		}
		public static long NewRevision(this CommitAttempt attempt)
		{
			return attempt.PreviousStreamRevision + attempt.Events.Count;
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
	}
}