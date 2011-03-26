namespace EventStore
{
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
			container.Register<IDispatchCommits>(new NullDispatcher());
			container.Register<IStoreEvents>(c =>
			{
				var concurrentHook = new OptimisticCommitHook();

				return new OptimisticEventStore(
					c.Resolve<IPersistStreams>(),
					c.Resolve<IDispatchCommits>(),
					new[] { concurrentHook },
					new[] { concurrentHook });
			});

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
	}
}