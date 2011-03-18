namespace EventStore
{
	using System;
	using Dispatcher;
	using Persistence;

	public class AsynchronousDispatcherWireup : Wireup
	{
		public AsynchronousDispatcherWireup(
			Wireup wireup,
			IPublishMessages publisher,
			Action<Commit, Exception> exceptionHandler)
			: base(wireup)
		{
			this.Container.Register(publisher);
			this.Container.Register(c => new AsynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>(), exceptionHandler));
		}

		public AsynchronousDispatcherWireup WithPublisher(IPublishMessages instance)
		{
			this.Container.Register(instance);
			return this;
		}

		public AsynchronousDispatcherWireup WithExceptionHandler(Action<Commit, Exception> instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}