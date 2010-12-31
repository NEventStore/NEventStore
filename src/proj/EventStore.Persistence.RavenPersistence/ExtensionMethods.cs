namespace EventStore.Persistence.RavenPersistence
{
	using System.Globalization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}
	}
}