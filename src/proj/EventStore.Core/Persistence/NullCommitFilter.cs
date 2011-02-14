namespace EventStore.Persistence
{
	public class NullCommitFilter : IFilterCommitReads, IFilterCommitWrites
	{
		public Commit FilterRead(Commit persisted)
		{
			return persisted;
		}
		public Commit FilterWrite(Commit attempt)
		{
			return attempt;
		}
	}
}