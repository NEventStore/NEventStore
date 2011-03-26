namespace EventStore
{
	using Dispatcher;

	public class DispatchCommitHook : IHookCommitAttempts
	{
		private readonly IDispatchCommits dispatcher;

		public DispatchCommitHook(IDispatchCommits dispatcher)
		{
			this.dispatcher = dispatcher;
		}

		public virtual bool PreCommit(Commit attempt)
		{
			return true;
		}
		public void PostCommit(Commit persisted)
		{
			this.dispatcher.Dispatch(persisted);
		}
	}
}