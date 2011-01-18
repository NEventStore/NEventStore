namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	internal static class ExtensionMethods
	{
		public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader, Func<IDataRecord, T> select)
		{
			while (reader.Read())
				yield return select(reader);
		}
	}
}