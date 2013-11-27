namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class AsynchronousDispatchSchedulerWireupExtensions
    {
        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            DispatcherSchedulerStartup schedulerStartup = DispatcherSchedulerStartup.Auto)
        {
            return wireup.UsingAsynchronousDispatchScheduler(null, schedulerStartup);
        }

        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher,
            DispatcherSchedulerStartup schedulerStartup = DispatcherSchedulerStartup.Auto)
        {
            return new AsynchronousDispatchSchedulerWireup(wireup, dispatcher, schedulerStartup);
        }
    }
}