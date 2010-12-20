namespace EventStore.Dispatcher
{
	using Persistence;

	public interface IPublishMessages
	{
		void Publish(Commit commit);
	}
}