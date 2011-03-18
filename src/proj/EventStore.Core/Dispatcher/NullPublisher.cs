namespace EventStore.Dispatcher
{
	public class NullPublisher : IPublishMessages
	{
		public void Dispose()
		{
		}
		public void Publish(Commit commit)
		{
		}
	}
}