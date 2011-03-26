namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using Dispatcher;
	using Persistence;

	public class Wireup
	{
		private readonly Wireup inner;
		private readonly NanoContainer container;

		protected Wireup(NanoContainer container)
		{
			this.container = container;
		}
		protected Wireup(Wireup inner)
		{
			this.inner = inner;
		}

		public static Wireup Init()
		{
			var container = new NanoContainer();

			container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
			container.Register(BuildEventStore);

			return new Wireup(container);
		}

		protected NanoContainer Container
		{
			get { return this.container ?? this.inner.Container; }
		}

		public virtual Wireup With<T>(T instance) where T : class
		{
			this.Container.Register(instance);
			return this;
		}

		public virtual IStoreEvents Build()
		{
			if (this.inner != null)
				return this.inner.Build();

			return this.Container.Resolve<IStoreEvents>();
		}

		private static IStoreEvents BuildEventStore(NanoContainer context)
		{
			var concurrentHook = new OptimisticReadCommitHook();
			var dispatcherHook = new DispatchCommitHook(context.Resolve<IDispatchCommits>());

			var readHooks = context.Resolve<IEnumerable<IReadHook>>() ?? new IReadHook[0];
			readHooks = new IReadHook[] { concurrentHook } .Concat(readHooks).ToArray();

			var commitHooks = context.Resolve<IEnumerable<ICommitHook>>() ?? new ICommitHook[0];
			commitHooks = new ICommitHook[] { concurrentHook, dispatcherHook } .Concat(commitHooks).ToArray();

			return new OptimisticEventStore(context.Resolve<IPersistStreams>(), commitHooks, readHooks);
		}
	}
}