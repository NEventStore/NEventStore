namespace EventStore.Dispatcher
{
	public sealed class NullDispatcher : IDispatchCommits,
		IPublishMessages
	{
		public void Dispose()
		{
		}
		public void Publish(Commit commit)
		{
		}
		public void Dispatch(Commit commit)
		{
		}
	}
}