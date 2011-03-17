namespace EventStore
{
	using Dispatcher;

	public class SynchronousDispatcherWireup : DispatcherWireup
	{
		private IPublishMessages publisher;

		public SynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			this.publisher = publisher;
		}

		public SynchronousDispatcherWireup WithPublisher(IPublishMessages instance)
		{
			this.publisher = instance;
			return this;
		}

		public override IStoreEvents Build()
		{
			this.WithDispatcher(new SynchronousDispatcher(this.publisher, this.Persistence));
			return base.Build();
		}
	}
}