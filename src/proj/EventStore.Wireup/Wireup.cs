namespace EventStore
{
	using Dispatcher;
	using Persistence;

	public class Wireup
	{
		public static Wireup Init()
		{
			return new Wireup();
		}

		protected Wireup()
		{
			this.Persistence = new InMemoryPersistenceEngine();
		}

		protected virtual IPersistStreams Persistence { get; private set; }
		public virtual Wireup WithPersistence(IPersistStreams instance)
		{
			this.Persistence = instance; // TODO: null check
			return this;
		}

		protected virtual IDispatchCommits Dispatcher { get; private set; }
		public virtual Wireup WithDispatcher(IDispatchCommits instance)
		{
			this.Dispatcher = instance; // TODO: null check
			return this;
		}

		public virtual IStoreEvents Build()
		{
			// TODO: assert that persistence and dispatcher have been configured
			return new OptimisticEventStore(this.Persistence, this.Dispatcher);
		}
	}
}