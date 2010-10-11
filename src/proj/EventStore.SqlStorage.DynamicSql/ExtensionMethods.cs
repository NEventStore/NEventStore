namespace EventStore.SqlStorage.DynamicSql
{
	using System;
	using System.Globalization;
	using System.Text;

	internal static class ExtensionMethods
	{
		public static byte[] ToNull(this Guid value)
		{
			return value == Guid.Empty ? null : value.ToByteArray();
		}
		public static object ToNull(this long value)
		{
			return value == 0 ? null : (object)value;
		}

		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static string Append(this string value, IConvertible number)
		{
			return value + number.ToString(CultureInfo.InvariantCulture);
		}

		public static void AppendWithFormat(this StringBuilder builder, string format, params object[] values)
		{
			builder.AppendFormat(CultureInfo.InvariantCulture, format, values);
		}
	}
}