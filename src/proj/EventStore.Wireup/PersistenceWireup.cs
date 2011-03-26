namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;
		private ICollection<IReadHook> readHooks;
		private ICollection<ICommitHook> commitHooks;

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

		public virtual PersistenceWireup InitializeDatabaseSchema()
		{
			this.initialize = true;
			return this;
		}

		public virtual PersistenceWireup FilterReadsUsing(IEnumerable<IReadHook> filters)
		{
			this.readHooks = (filters ?? new IReadHook[] { }).Where(x => x != null).ToArray();
			return this;
		}
		public virtual PersistenceWireup FilterWritesUsing(IEnumerable<ICommitHook> filters)
		{
			this.commitHooks = (filters ?? new ICommitHook[] { }).Where(x => x != null).ToArray();
			return this;
		}

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			if (this.readHooks.Count > 0)
				this.Container.Register(this.readHooks);

			if (this.commitHooks.Count > 0)
				this.Container.Register(this.commitHooks);

			return base.Build();
		}
	}
}