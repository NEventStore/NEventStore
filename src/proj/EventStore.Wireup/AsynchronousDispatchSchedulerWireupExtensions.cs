namespace EventStore
{
	using Dispatcher;

	public static class AsynchronousDispatchSchedulerWireupExtensions
	{
		public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(this Wireup wireup)
		{
			return wireup.UsingAsynchronousDispatchScheduler(null);
		}
		public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
			this Wireup wireup, IDispatchCommits dispatcher)
		{
			return new AsynchronousDispatchSchedulerWireup(wireup, dispatcher);
		}
	}
}