namespace EventStore.Example
{
	public class PreventUnauthorizedWritesCommitHook : ICommitHook
	{
		public bool PreCommit(Commit attempt)
		{
			// Can easily do logging or other such activities here
			return true; // true == allow commit to continue, false = stop.
		}
		public void PostCommit(Commit committed)
		{
			// anything to do after the commit has been persisted.
		}
	}
}