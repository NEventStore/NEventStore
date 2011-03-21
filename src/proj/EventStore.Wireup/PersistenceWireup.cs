namespace EventStore
{
	using Persistence;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;

		public PersistenceWireup(Wireup inner)
			: base(inner)
		{
		}

		public override IWireup WithPersistence(IPersistStreams instance)
		{
			return this.WithPersistence(instance);
		}

		public virtual PersistenceWireup InitializeDatabaseSchema()
		{
			this.initialize = true;
			return this;
		}

		public override IStoreEvents Build()
		{
			if (this.initialize)
				this.Container.Resolve<IPersistStreams>().Initialize();

			return base.Build();
		}
	}
}