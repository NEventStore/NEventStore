namespace EventStore.Core.Sql
{
	using System;
	using System.Globalization;
	using System.Text;

	internal static class StringFormattingExtensions
	{
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

		public static string ToNull(this Guid value)
		{
			return Guid.Empty == value ? null : value.ToString();
		}
	}
}