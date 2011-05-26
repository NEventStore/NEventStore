namespace EventStore
{
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;

		public PersistenceWireup(Wireup inner)
			: this(inner, null)
		{
		}
		public PersistenceWireup(Wireup inner, IPersistStreams instance)
			: base(inner)
		{
			if (inner != null && instance != null)
				inner.With(instance);
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

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			return base.Build();
		}
	}
}