namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	internal static class ExtensionMethods
	{
		public static string NonPaged(this string statement)
		{
			return statement.Replace("LIMIT @Skip, @Limit;", ";");
		}
	}
}