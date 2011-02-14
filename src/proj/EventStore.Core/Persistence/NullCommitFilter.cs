namespace EventStore.Persistence
{
	public class NullCommitFilter : IFilterCommitReads, IFilterCommitWrites
	{
		public Commit FilterRead(Commit committed)
		{
			return committed;
		}
		public Commit FilterWrite(Commit attempt)
		{
			return attempt;
		}
	}
}