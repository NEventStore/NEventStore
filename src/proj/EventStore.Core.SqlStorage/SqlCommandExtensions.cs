namespace EventStore.Core.SqlStorage
{
	using System;
	using System.Data;

	internal static class SqlCommandExtensions
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