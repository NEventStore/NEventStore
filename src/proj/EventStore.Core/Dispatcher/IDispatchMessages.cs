namespace EventStore.Dispatcher
{
	using System;

	public interface IPublishMessages : IDisposable
	{
		void Publish(Commit commit);
	}
}