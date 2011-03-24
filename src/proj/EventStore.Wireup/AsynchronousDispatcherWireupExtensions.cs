namespace EventStore
{
	using Dispatcher;

	public static class AsynchronousDispatcherWireupExtensions
	{
		public static AsynchronousDispatcherWireup UsingAsynchronousDispatcher(this Wireup wireup)
		{
			return wireup.UsingAsynchronousDispatcher(null);
		}
		public static AsynchronousDispatcherWireup UsingAsynchronousDispatcher(this Wireup wireup, IPublishMessages publisher)
		{
			return new AsynchronousDispatcherWireup(wireup, publisher);
		}
	}
}