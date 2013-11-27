namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class AsynchronousDispatchSchedulerWireupExtensions
    {
        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            DispatcherStartup startup = DispatcherStartup.Auto)
        {
            return wireup.UsingAsynchronousDispatchScheduler(null, startup);
        }

        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher,
            DispatcherStartup startup = DispatcherStartup.Auto)
        {
            return new AsynchronousDispatchSchedulerWireup(wireup, dispatcher, startup);
        }
    }
}