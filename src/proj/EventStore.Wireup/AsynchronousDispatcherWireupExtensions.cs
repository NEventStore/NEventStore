namespace EventStore
{
	using System;
	using Dispatcher;

	public static class AsynchronousDispatcherWireupExtensions
	{
		public static AsynchronousDispatcherWireup UsingAsynchronousDispatcher(this Wireup wireup, IPublishMessages publisher)
		{
			return wireup.UsingAsynchronousDispatcher(publisher, (c, e) => { });
		}
		public static AsynchronousDispatcherWireup UsingAsynchronousDispatcher(
			this Wireup wireup, IPublishMessages publisher, Action<Commit, Exception> exceptionHandler)
		{
			return new AsynchronousDispatcherWireup(wireup, publisher, exceptionHandler);
		}
	}
}