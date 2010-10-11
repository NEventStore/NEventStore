namespace EventStore.SqlStorage.DynamicSql
{
	public interface IAdaptDynamicSqlDialect : IAdaptSqlDialect
	{
		string GetSelectEventsQuery { get; }
		string GetSelectEventsForCommandQuery { get; }
		string GetSelectEventsForVersionQuery { get; }
		string GetInsertEventsCommand { get; }
		string GetInsertEventCommand { get; }
	}
}