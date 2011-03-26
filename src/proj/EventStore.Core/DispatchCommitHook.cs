namespace EventStore
{
	using Dispatcher;

	public class DispatchCommitHook : ICommitHook
	{
		private readonly IDispatchCommits dispatcher;

		public DispatchCommitHook()
			: this(null)
		{
		}
		public DispatchCommitHook(IDispatchCommits dispatcher)
		{
			this.dispatcher = dispatcher ?? new NullDispatcher();
		}

		public virtual bool PreCommit(Commit attempt)
		{
			return true;
		}
		public void PostCommit(Commit committed)
		{
			if (committed != null)
				this.dispatcher.Dispatch(committed);
		}
	}
}