namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private ICollection<IPipelineHook> pipelineHooks = new IPipelineHook[] { };
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

		public virtual PersistenceWireup InitializeDatabaseSchema()
		{
			this.initialize = true;
			return this;
		}

		public virtual PersistenceWireup HookIntoPipelineUsing(params IPipelineHook[] hooks)
		{
			this.pipelineHooks = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
			return this;
		}
		public virtual PersistenceWireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
		{
			this.pipelineHooks = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
			return this;
		}

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			if (this.pipelineHooks.Count > 0)
				this.Container.Register(this.pipelineHooks);

			return base.Build();
		}
	}
}