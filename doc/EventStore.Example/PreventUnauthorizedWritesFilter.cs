namespace EventStore.Example
{
	using Persistence;

	public class PreventUnauthorizedWritesFilter : IFilterCommitWrites
	{
		public Commit FilterWrite(Commit committed)
		{
			// Can easily do logging or other such activities here
			// Simply return null to suppress writing to the event store.
			return committed;
		}
	}
}