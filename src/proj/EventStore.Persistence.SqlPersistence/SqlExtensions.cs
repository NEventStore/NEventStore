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

			if (value is Guid)
				parameter.DbType = DbType.Guid;
			else if (value is string)
			{
				parameter.DbType = DbType.String;
				value = ((string)value).ToNull();
			}
			else if (value is long)
				parameter.DbType = DbType.Int64;
			else if (value is byte[])
				parameter.DbType = DbType.Binary;

			parameter.Value = value ?? DBNull.Value;
			command.Parameters.Add(parameter);
		}

		public static int ExecuteAndSuppressExceptions(this IDbCommand command)
		{
			return command.ExecuteAndSuppressExceptions(new[] { command.CommandText }, null);
		}
		public static int ExecuteAndSuppressExceptions(this IDbCommand command, IEnumerable<string> statements, ISqlDialect dialect)
		{
			try
			{
				return command.ExecuteNonQuery(statements, dialect);
			}
			catch (Exception)
			{
				return 0;
			}
		}
		public static int ExecuteNonQuery(this IDbCommand command, IEnumerable<string> statements, ISqlDialect dialect)
		{
			return statements.Sum(statement =>
			{
				command.CommandText = statement;

				if (dialect != null)
					dialect.AmmendStatement(command);

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