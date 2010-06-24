namespace EventStore.Core.Sql
{
	using System.Data;

	internal static class CommandExtensions
	{
		public static void AddWithValue(this IDbCommand command, string parameterName, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			command.Parameters.Add(parameter);
		}
	}
}