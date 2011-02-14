namespace EventStore.Persistence
{
	public interface IFilterCommitWrites
	{
		Commit FilterWrite(Commit attempt);
	}
}