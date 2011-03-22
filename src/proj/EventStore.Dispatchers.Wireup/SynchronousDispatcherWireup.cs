namespace EventStore
{
	using Dispatcher;
	using Persistence;

	public class SynchronousDispatcherWireup : Wireup
	{
		public SynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			this.PublishTo(publisher ?? new NullPublisher());
			this.Container.Register<IDispatchCommits>(c => new SynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>()));
		}

		public SynchronousDispatcherWireup PublishTo(IPublishMessages instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}