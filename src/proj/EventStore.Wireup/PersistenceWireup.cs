namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private bool initialize;
		private ICollection<IFilterCommitReads> readFilters;
		private ICollection<IFilterCommitWrites> writeFilters;

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

		public virtual PersistenceWireup FilterReadsUsing(IEnumerable<IFilterCommitReads> filters)
		{
			this.readFilters = (filters ?? new IFilterCommitReads[] { }).Where(x => x != null).ToArray();
			return this;
		}
		public virtual PersistenceWireup FilterWritesUsing(IEnumerable<IFilterCommitWrites> filters)
		{
			this.writeFilters = (filters ?? new IFilterCommitWrites[] { }).Where(x => x != null).ToArray();
			return this;
		}

		public override IStoreEvents Build()
		{
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
				engine.Initialize();

			if (this.readFilters.Count > 0 || this.writeFilters.Count > 0)
				this.Container.Register(new CommitFilterPersistence(engine, this.readFilters, this.writeFilters));

			return base.Build();
		}
	}
}