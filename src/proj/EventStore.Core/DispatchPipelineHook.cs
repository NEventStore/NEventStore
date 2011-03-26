namespace EventStore
{
	using Dispatcher;

	public class DispatchPipelineHook : IPipelineHook
	{
		private readonly IDispatchCommits dispatcher;

		public DispatchPipelineHook()
			: this(null)
		{
		}
		public DispatchPipelineHook(IDispatchCommits dispatcher)
		{
			this.dispatcher = dispatcher ?? new NullDispatcher();
		}

		public Commit Select(Commit committed)
		{
			return committed;
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