namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class SynchronousDispatcherWireupExtensions
    {
        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            DispatcherSchedulerStartup schedulerStartup = DispatcherSchedulerStartup.Auto)
        {
            return wireup.UsingSynchronousDispatchScheduler(null, schedulerStartup);
        }

        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher,
            DispatcherSchedulerStartup schedulerStartup = DispatcherSchedulerStartup.Auto)
        {
            return new SynchronousDispatchSchedulerWireup(wireup, dispatcher, schedulerStartup);
        }
    }
}