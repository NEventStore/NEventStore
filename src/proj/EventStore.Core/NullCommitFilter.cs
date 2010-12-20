namespace EventStore.Core
{
	public class NullCommitFilter<T> : IFilterCommits<T>
	{
		public T Filter(T commit)
		{
			return commit;
		}
	}
}