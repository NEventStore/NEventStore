namespace NEventStore
{
    using System.Transactions;
    using NEventStore.Dispatcher;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class AsynchronousDispatchSchedulerWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (AsynchronousDispatchSchedulerWireup));

        public AsynchronousDispatchSchedulerWireup(Wireup wireup, IDispatchCommits dispatcher)
            : base(wireup)
        {
            var option = Container.Resolve<TransactionScopeOption>();
            if (option != TransactionScopeOption.Suppress)
            {
                Logger.Warn(Messages.SynchronousDispatcherTwoPhaseCommits);
            }

            Logger.Debug(Messages.AsyncDispatchSchedulerRegistered);
            DispatchTo(dispatcher ?? new NullDispatcher());
            Container.Register<IScheduleDispatches>(c => new AsynchronousDispatchScheduler(
                c.Resolve<IDispatchCommits>(), c.Resolve<IPersistStreams>()));
        }

        public AsynchronousDispatchSchedulerWireup DispatchTo(IDispatchCommits instance)
        {
            Logger.Debug(Messages.DispatcherRegistered, instance.GetType());
            Container.Register(instance);
            return this;
        }
    }
}