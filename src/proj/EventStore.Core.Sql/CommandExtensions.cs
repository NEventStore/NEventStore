namespace EventStore.Core.Sql
{
	using System.Data;

	internal static class CommandExtensions
	{
		public static IDataParameter AddWithValue(this IDbCommand command, string parameterName, object value)
		{
			return command.AddWithValue(parameterName, value, ParameterDirection.Input);
		}
		public static IDataParameter AddWithValue(
			this IDbCommand command, string parameterName, object value, ParameterDirection direction)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			parameter.Direction = direction;
			command.Parameters.Add(parameter);
			return parameter;
		}
	}
}