namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Linq;

	internal static class SqlExtensions
	{
		public static IDataParameter AddParameter(this IDbCommand command, string parameterName, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = (value is Guid) ? ((Guid)value).ToByteArray() : value ?? DBNull.Value;
			command.Parameters.Add(parameter);
			return parameter;
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

		public static int ForEach(this IDbCommand command, Func<int> callback)
		{
			return command.EnumerateStatements().Sum(statement =>
			{
				command.CommandText = statement;
				return callback();
			});
		}
		public static void ForEach(this IDbCommand command, Action callback)
		{
			foreach (var statement in command.EnumerateStatements())
			{
				command.CommandText = statement;
				callback();
			}
		}
		private static IEnumerable<string> EnumerateStatements(this IDbCommand command)
		{
			return command.CommandText.Split(new[] { "/**/" }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}