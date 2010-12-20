namespace EventStore.Core
{
	using Persistence;

	public class NullCommitFilter : IFilterCommits
	{
		public Commit Filter(Commit commit)
		{
			return commit;
		}
	}
}