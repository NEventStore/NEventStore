namespace EventStore
{
	using System;
	using Dispatcher;

	public class AsynchronousDispatcherWireup : DispatcherWireup
	{
		private Action<Commit, Exception> handler = (c, e) => { };
		private IPublishMessages publisher;

		public AsynchronousDispatcherWireup(Wireup wireup, IPublishMessages publisher)
			: base(wireup)
		{
			this.publisher = publisher;
		}

		public AsynchronousDispatcherWireup WithPublisher(IPublishMessages instance)
		{
			this.publisher = instance;
			return this;
		}

		public AsynchronousDispatcherWireup RouteErrorsTo(Action<Commit, Exception> instance)
		{
			this.handler = instance;
			return this;
		}

		public override IStoreEvents Build()
		{
			this.WithDispatcher(new AsynchronousDispatcher(this.publisher, this.Persistence, this.handler));
			return base.Build();
		}
	}
}