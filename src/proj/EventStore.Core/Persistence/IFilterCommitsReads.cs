namespace EventStore.Persistence
{
	public interface IFilterCommitReads
	{
		Commit FilterRead(Commit persisted);
	}
}