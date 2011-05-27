namespace EventStore
{
	using System.Transactions;
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;

		public PersistenceWireup(Wireup inner)
			: base(inner)
		{
			this.Container.Register(TransactionScopeOption.Suppress);
		}

		public virtual PersistenceWireup WithPersistence(IPersistStreams instance)
		{
			this.With(instance);
			return this;
		}

		protected virtual SerializationWireup WithSerializer(ISerialize serializer)
		{
			return new SerializationWireup(this, serializer);
		}

		public virtual PersistenceWireup InitializeStorageEngine()
		{
			this.initialize = true;
			return this;
		}

		public virtual PersistenceWireup EnlistInAmbientTransaction()
		{
			this.Container.Register(TransactionScopeOption.Required);
			return this;
		}

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			return base.Build();
		}
	}
}