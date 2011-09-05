namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;
	using System.Globalization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
		}

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
		public static DateTime ToDateTime(this object value)
		{
			value = value is decimal ? (long)(decimal)value : value;
			return value is long ? new DateTime((long)value) : (DateTime)value;
		}

		public static IDbCommand SetParameter(this IDbCommand command, string name, object value)
		{
			var parameter = (IDataParameter)command.Parameters[name];
			parameter.Value = value;
			return command;
		}
	}
}