namespace EventStore.SqlPersistence
{
	internal static class ExtensionMethods
	{
		public static string ToNull(this string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}
	}
}