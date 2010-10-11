namespace EventStore.SqlStorage
{
	using System;
	using System.Data;

	public static class ExtensionMethods
	{
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