namespace EventStore.SqlStorage.DynamicSql
{
	public interface IAdaptDynamicSqlDialect : IAdaptSqlDialect
	{
		string GetSelectEventsQuery { get; }
		string GetSelectEventsForCommandQuery { get; }
		string GetSelectEventsSinceVersionQuery { get; }
		string GetInsertEventsCommand { get; }
		string GetInsertEventCommand { get; }
	}
}