namespace NEventStore
{
    using NEventStore.Dispatcher;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class SynchronousDispatchSchedulerWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SynchronousDispatchSchedulerWireup));

        public SynchronousDispatchSchedulerWireup(Wireup wireup, IDispatchCommits dispatcher, DispatcherStartup startup)
            : base(wireup)
        {
            Logger.Debug(Messages.SyncDispatchSchedulerRegistered);
            DispatchTo(dispatcher ?? new NullDispatcher());
            Container.Register<IScheduleDispatches>(c =>
            {
                var dispatchScheduler = new SynchronousDispatchScheduler(
                    c.Resolve<IDispatchCommits>(),
                    c.Resolve<IPersistStreams>());
                if (startup == DispatcherStartup.Auto)
                {
                    dispatchScheduler.Start();
                }
                return dispatchScheduler;
            });
        }

        public SynchronousDispatchSchedulerWireup DispatchTo(IDispatchCommits instance)
        {
            Logger.Debug(Messages.DispatcherRegistered, instance.GetType());
            Container.Register(instance);
            return this;
        }
    }
}