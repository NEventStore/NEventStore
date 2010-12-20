namespace EventStore.Core
{
	public interface IFilterCommits<T>
	{
		T Filter(T commit);
	}
}