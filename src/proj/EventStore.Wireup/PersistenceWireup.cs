namespace EventStore
{
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;

		public PersistenceWireup(Wireup inner)
			: base(inner)
		{
		}

		public virtual Wireup WithPersistence(IPersistStreams instance)
		{
			return this.With(instance);
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

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			return base.Build();
		}
	}
}