namespace EventStore.SqlPersistence
{
	using System;

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
	}
}