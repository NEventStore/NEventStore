namespace EventStore
{
	using Persistence;

	public class PersistenceWireup : Wireup
	{
		private readonly Wireup inner;
		private bool initialize;

		public PersistenceWireup(Wireup inner)
		{
			this.inner = inner;
		}

		public override Wireup WithPersistence(IPersistStreams instance)
		{
			return this.inner.WithPersistence(instance);
		}

		public virtual PersistenceWireup InitalizePersistence()
		{
			this.initialize = true;
			return this;
		}

		public override IStoreEvents Build()
		{
			if (this.initialize)
				this.Persistence.Initialize();

			return this.inner.Build();
		}
	}
}