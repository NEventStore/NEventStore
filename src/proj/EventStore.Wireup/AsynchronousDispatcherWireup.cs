namespace EventStore
{
	using System.Transactions;
	using Dispatcher;
	using Logging;
	using Persistence;

	public class AsynchronousDispatcherWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatcherWireup));

		public AsynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			var option = this.Container.Resolve<TransactionScopeOption>();
			if (option == TransactionScopeOption.Required)
				Logger.Warn(Messages.SynchronousDispatcherTwoPhaseCommits);

			Logger.Debug(Messages.AsyncDispatcherRegistered);
			this.PublishTo(publisher ?? new NullPublisher());
			this.Container.Register<IDispatchCommits>(c => new AsynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>()));
		}

		public AsynchronousDispatcherWireup PublishTo(IPublishMessages instance)
		{
			Logger.Debug(Messages.MessagePublisherRegistered, instance.GetType());
			this.Container.Register(instance);
			return this;
		}
	}
}