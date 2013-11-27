namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class SynchronousDispatcherWireupExtensions
    {
        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            DispatcherStartup startup = DispatcherStartup.Auto)
        {
            return wireup.UsingSynchronousDispatchScheduler(null, startup);
        }

        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher,
            DispatcherStartup startup = DispatcherStartup.Auto)
        {
            return new SynchronousDispatchSchedulerWireup(wireup, dispatcher, startup);
        }
    }
}