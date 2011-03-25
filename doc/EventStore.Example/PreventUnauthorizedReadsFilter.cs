namespace EventStore.Example
{
	using Persistence;

	public class PreventUnauthorizedReadsFilter : IFilterCommitReads
	{
		public Commit FilterRead(Commit committed)
		{
			// Authorization or other logging may be done here.
			return committed;
		}
	}
}