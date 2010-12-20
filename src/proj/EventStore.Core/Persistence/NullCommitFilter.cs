namespace EventStore.Persistence
{
	public class NullCommitFilter<T> : IFilterCommits<T>
	{
		public T Filter(T commit)
		{
			return commit;
		}
	}
}