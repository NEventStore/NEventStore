namespace EventStore
{
	using Dispatcher;
	using Persistence;

	public class AsynchronousDispatcherWireup : Wireup
	{
		public AsynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			this.PublishTo(publisher ?? new NullDispatcher());
			this.Container.Register<IDispatchCommits>(c => new AsynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>()));
		}

		public AsynchronousDispatcherWireup PublishTo(IPublishMessages instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}