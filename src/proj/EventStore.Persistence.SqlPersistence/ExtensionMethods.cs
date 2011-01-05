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
		public static int ToInt(this object value)
		{
			return value is long ? (int)(long)value : (int)value;
		}
	}
}