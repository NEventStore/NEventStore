namespace EventStore
{
	using System.Transactions;
	using Dispatcher;
	using Logging;
	using Persistence;

	public class AsynchronousDispatchSchedulerWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatchSchedulerWireup));

		public AsynchronousDispatchSchedulerWireup(Wireup wireup, IDispatchCommits dispatcher)
			: base(wireup)
		{
			var option = this.Container.Resolve<TransactionScopeOption>();
			if (option == TransactionScopeOption.Required)
				Logger.Warn(Messages.SynchronousDispatcherTwoPhaseCommits);

			Logger.Debug(Messages.AsyncDispatchSchedulerRegistered);
			this.DispatchTo(dispatcher ?? new NullDispatcher());
			this.Container.Register<IScheduleDispatches>(c => new AsynchronousDispatchScheduler(
				c.Resolve<IDispatchCommits>(), c.Resolve<IPersistStreams>()));
		}

		public AsynchronousDispatchSchedulerWireup DispatchTo(IDispatchCommits instance)
		{
			Logger.Debug(Messages.DispatcherRegistered, instance.GetType());
			this.Container.Register(instance);
			return this;
		}
	}
}