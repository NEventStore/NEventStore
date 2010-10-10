namespace EventStore.Core.SqlStorage
{
	using System;
	using System.Data;

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

		public static IDataParameter AddParameter(this IDbCommand command, string parameterName, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value ?? DBNull.Value;

			if (parameter.Value == DBNull.Value)
				parameter.DbType = DbType.Binary;

			command.Parameters.Add(parameter);
			return parameter;
		}
	}
}