namespace EventStore
{
	using Dispatcher;
	using Logging;
	using Persistence;

	public class SynchronousDispatcherWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SynchronousDispatcherWireup));

		public SynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			Logger.Debug(Messages.SyncDispatcherRegistered);
			this.PublishTo(publisher ?? new NullDispatcher());
			this.Container.Register<IDispatchCommits>(c => new SynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>()));
		}

		public SynchronousDispatcherWireup PublishTo(IPublishMessages instance)
		{
			Logger.Debug(Messages.MessagePublisherRegistered, instance.GetType());
			this.Container.Register(instance);
			return this;
		}
	}
}