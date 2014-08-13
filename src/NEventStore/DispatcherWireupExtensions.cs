namespace NEventStore
{
    using System;
    using NEventStore.Dispatcher;

    [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
    public static class DispatcherWireupExtensions
    {
        [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(this Wireup wireup)
        {
            return wireup.UsingSynchronousDispatchScheduler(null);
        }

        [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
        public static SynchronousDispatchSchedulerWireup UsingSynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher)
        {
            return new SynchronousDispatchSchedulerWireup(wireup, dispatcher, DispatcherSchedulerStartup.Auto);
        }

        [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup)
        {
            return wireup.UsingAsynchronousDispatchScheduler(null);
        }

        [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
        public static AsynchronousDispatchSchedulerWireup UsingAsynchronousDispatchScheduler(
            this Wireup wireup,
            IDispatchCommits dispatcher)
        {
            return new AsynchronousDispatchSchedulerWireup(wireup, dispatcher, DispatcherSchedulerStartup.Auto);
        }

        [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
        public static NoopDispatchSchedulerWireup DoNotDispatchCommits(this Wireup wireup)
        {
            return new NoopDispatchSchedulerWireup(wireup);
        }
    }
}