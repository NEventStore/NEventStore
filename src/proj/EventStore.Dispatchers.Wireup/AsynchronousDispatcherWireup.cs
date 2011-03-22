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
			this.PublishTo(publisher ?? new NullPublisher());
			this.Container.Register<IDispatchCommits>(c => new AsynchronousDispatcher(
				c.Resolve<IPublishMessages>(), c.Resolve<IPersistStreams>(), exceptionHandler));
		}

		public AsynchronousDispatcherWireup PublishTo(IPublishMessages instance)
		{
			this.Container.Register(instance);
			return this;
		}

		public AsynchronousDispatcherWireup HandleExceptionsWith(Action<Commit, Exception> instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}