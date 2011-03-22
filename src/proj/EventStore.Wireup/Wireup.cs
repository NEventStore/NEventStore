namespace EventStore
{
	using Dispatcher;
	using Persistence;
	using Serialization;

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
			container.Register<ISerialize>(new BinarySerializer());
			container.Register<IStoreEvents>(c => new OptimisticEventStore(
				c.Resolve<IPersistStreams>(), c.Resolve<IDispatchCommits>()));

			return new Wireup(container);
		}

		protected NanoContainer Container
		{
			get { return this.container ?? this.inner.Container; }
		}

		public virtual void With<T>(T instance) where T : class
		{
			this.Container.Register(instance);
		}

		public virtual IStoreEvents Build()
		{
			return this.Container.Resolve<IStoreEvents>();
		}
	}
}