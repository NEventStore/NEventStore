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
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;

			//// MySQL
			//// else if (value is Guid && type == DbType.Binary)
			////    value = ((Guid)value).ToByteArray();
			if (value is Guid)
			{
				parameter.DbType = DbType.Guid;
				value = (Guid)value;
			}
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

		public static int ExecuteAndSuppressExceptions(this IDbCommand command, params string[] statements)
		{
			try
			{
				return command.ExecuteNonQuery(statements);
			}
			catch (Exception)
			{
				return 0;
			}
		}
		public static int ExecuteNonQuery(this IDbCommand command, params string[] statements)
		{
			statements = statements ?? new string[] { };
			if (statements.Length == 0)
				return command.ExecuteNonQuery();

			return statements.Sum(statement =>
			{
				command.CommandText = statement;
				return command.ExecuteNonQuery();
			});
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