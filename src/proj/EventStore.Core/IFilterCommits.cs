namespace EventStore.Core
{
	using Persistence;

	public interface IFilterCommits
	{
		Commit Filter(Commit commit);
	}
}