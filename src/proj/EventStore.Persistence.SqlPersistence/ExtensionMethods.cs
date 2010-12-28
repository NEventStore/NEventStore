namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;

	internal static class ExtensionMethods
	{
		public static string ToNull(this string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}
		public static Guid ToGuid(this object value)
		{
			if (value is Guid)
				return (Guid)value;

			var bytes = value as byte[];
			return bytes != null ? new Guid(bytes) : Guid.Empty;
		}

		public static void ForEach<T>(this IEnumerable<T> values, Action<T> callback)
		{
			foreach (var value in values)
				callback(value);
		}
	}
}