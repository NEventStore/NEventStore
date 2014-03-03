namespace NEventStore
{
    using NEventStore.Dispatcher;

    public static class DispatcherWireupExtensions
    {
        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(this Wireup wireup)
        {
            return wireup.UsingSynchronousDispatchScheduler(null);
        }

        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher)
        {
            return new SynchronousDispatchSchedulerWireup(wireup, dispatcher, DispatcherSchedulerStartup.Auto);
        }

        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup)
        {
            return wireup.UsingAsynchronousDispatchScheduler(null);
        }

        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher)
        {
            return new AsynchronousDispatchSchedulerWireup(wireup, dispatcher, DispatcherSchedulerStartup.Auto);
        }

        public static NoopDispatchSchedulerWireup DoNotDispatchCommits(this Wireup wireup)
        {
            return new NoopDispatchSchedulerWireup(wireup);
        }
    }
}