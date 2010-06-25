namespace EventStore.Core.Sql
{
	internal static class TypeExtensions
	{
		public static string GetTypeName(this object value)
		{
			return value == null ? string.Empty : value.GetType().FullName;
		}
	}
}