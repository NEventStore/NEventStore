namespace EventStore.Persistence
{
	public interface IFilterCommits<T>
	{
		T Filter(T commit);
	}
}