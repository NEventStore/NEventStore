namespace EventStore
{
	using Dispatcher;
	using Persistence;

	public class SynchronousDispatcherWireup : Wireup
	{
		public SynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			this.Container.Register(publisher);
			this.Container.Register(c => new SynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>()));
		}

		public SynchronousDispatcherWireup WithPublisher(IPublishMessages instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}