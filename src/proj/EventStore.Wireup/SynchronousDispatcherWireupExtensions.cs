namespace EventStore
{
	using Dispatcher;

	public static class SynchronousDispatcherWireupExtensions
	{
		public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(this Wireup wireup)
		{
			return wireup.UsingSynchronousDispatchScheduler(null);
		}

		public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
			this Wireup wireup, IDispatchCommits dispatcher)
		{
			return new SynchronousDispatchSchedulerWireup(wireup, dispatcher);
		}
	}
}