namespace EventStore.Core
{
	using System;
	using System.Globalization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}
		public static bool HasAttribute<T>(this Type type) where T : Attribute
		{
			return type.GetCustomAttributes(typeof(T), false).Length > 0;
		}
	}
}