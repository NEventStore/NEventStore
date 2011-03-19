namespace EventStore
{
	using Dispatcher;

	public static class SynchronousDispatcherWireupExtensions
	{
		public static SynchronousDispatcherWireup UsingSynchronousDispatcher(this Wireup wireup)
		{
			return wireup.UsingSynchronousDispatcher(null);
		}

		public static SynchronousDispatcherWireup UsingSynchronousDispatcher(this Wireup wireup, IPublishMessages publisher)
		{
			return new SynchronousDispatcherWireup(wireup, publisher);
		}
	}
}