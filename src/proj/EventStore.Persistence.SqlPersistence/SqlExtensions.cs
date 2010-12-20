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
			if (value is Guid)
				value = ((Guid)value).ToByteArray();

			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value ?? DBNull.Value;

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
	}
}