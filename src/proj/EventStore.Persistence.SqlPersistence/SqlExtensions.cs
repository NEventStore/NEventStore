namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	internal static class SqlExtensions
	{
		public static void AddParameter(this IDbCommand command, string parameterName, object value)
		{
			command.AddParameter(parameterName, value, DbType.Object);
		}
		public static void AddParameter(this IDbCommand command, string parameterName, object value, DbType type)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;

			if (value is Guid && type == DbType.Guid)
			{
				parameter.DbType = DbType.Guid;
				value = (Guid)value;
			}
			else if (value is Guid && type == DbType.Binary)
				value = ((Guid)value).ToByteArray();
			else if (value is string)
			{
				parameter.DbType = DbType.String;
				value = (object)((string)value).ToNull() ?? DBNull.Value;
			}
			else if (value is long)
				parameter.DbType = DbType.Int64;
			else if (value is byte[])
				parameter.DbType = DbType.Binary;

			parameter.Value = value ?? DBNull.Value;
			command.Parameters.Add(parameter);
		}

		public static void ExecuteAndSuppressExceptions(this IDbCommand command)
		{
			try
			{
				command.ExecuteNonQuery();
			}
			catch (Exception)
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