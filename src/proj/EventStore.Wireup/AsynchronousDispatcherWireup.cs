namespace EventStore
{
	using Dispatcher;
	using Logging;
	using Persistence;

	public class AsynchronousDispatcherWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatcherWireup));

		public AsynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			Logger.Debug(Messages.AsyncDispatcherRegistered);
			this.PublishTo(publisher ?? new NullDispatcher());
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