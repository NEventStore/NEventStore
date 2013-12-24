namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class DispatcherWireupExtensions
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

        public static NoopDispatchSchedulerWireup DoNotDispatchCommits(this Wireup wireup)
        {
            return new NoopDispatchSchedulerWireup(wireup);
        }
    }
}