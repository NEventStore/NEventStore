namespace EventStore.Dispatcher
{
	using System;
	using Persistence;

	public interface IPublishMessages : IDisposable
	{
		void Publish(Commit commit);
	}
}