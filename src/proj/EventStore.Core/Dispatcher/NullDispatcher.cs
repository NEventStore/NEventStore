namespace EventStore.Dispatcher
{
	public sealed class NullDispatcher : IDispatchCommits
	{
		public void Dispose()
		{
		}
		public void Dispatch(Commit commit)
		{
		}
	}
}