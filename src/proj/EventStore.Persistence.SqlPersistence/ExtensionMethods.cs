namespace EventStore.Persistence.SqlPersistence
{
	using System;

	internal static class ExtensionMethods
	{
		public static Guid ToGuid(this object value)
		{
			if (value is Guid)
				return (Guid)value;

			var bytes = value as byte[];
			return bytes != null ? new Guid(bytes) : Guid.Empty;
		}
		public static long ToLong(this object value)
		{
			if (value is int)
				return (int)value;

			return (long)value;
		}
	}
}