namespace EventStore.Example
{
	using System;
	using NEventStore;

    public class AuthorizationPipelineHook : IPipelineHook
	{
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		public Commit Select(Commit committed)
		{
			// return null if the user isn't authorized to see this commit
			return committed;
		}
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