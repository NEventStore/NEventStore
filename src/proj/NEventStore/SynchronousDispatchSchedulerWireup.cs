namespace EventStore
{
    using Dispatcher;
    using Logging;
    using Persistence;

    public class SynchronousDispatchSchedulerWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SynchronousDispatchSchedulerWireup));

		public SynchronousDispatchSchedulerWireup(Wireup wireup, IDispatchCommits dispatcher)
			: base(wireup)
		{
			Logger.Debug(Messages.SyncDispatchSchedulerRegistered);
			this.DispatchTo(dispatcher ?? new NullDispatcher());
			this.Container.Register<IScheduleDispatches>(c => new SynchronousDispatchScheduler(
				c.Resolve<IDispatchCommits>(), c.Resolve<IPersistStreams>()));
		}

		public SynchronousDispatchSchedulerWireup DispatchTo(IDispatchCommits instance)
		{
			Logger.Debug(Messages.DispatcherRegistered, instance.GetType());
			this.Container.Register(instance);
			return this;
		}
	}
}