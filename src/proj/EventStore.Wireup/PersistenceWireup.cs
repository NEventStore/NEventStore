namespace EventStore
{
	using System.Transactions;
	using Logging;
	using Persistence;
	using Serialization;

	public class PersistenceWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));
		private bool initialize;

		public PersistenceWireup(Wireup inner)
			: base(inner)
		{
			this.Container.Register(TransactionScopeOption.Suppress);
		}

		public virtual PersistenceWireup WithPersistence(IPersistStreams instance)
		{
			Logger.Debug(Messages.RegisteringPersistenceEngine, instance.GetType());
			this.With(instance);
			return this;
		}

		protected virtual SerializationWireup WithSerializer(ISerialize serializer)
		{
			return new SerializationWireup(this, serializer);
		}

		public virtual PersistenceWireup InitializeStorageEngine()
		{
			Logger.Debug(Messages.ConfiguringEngineInitialization);
			this.initialize = true;
			return this;
		}

		public virtual PersistenceWireup EnlistInAmbientTransaction()
		{
			Logger.Debug(Messages.ConfiguringEngineEnlistment);
			this.Container.Register(TransactionScopeOption.Required);
			return this;
		}

		public override IStoreEvents Build()
		{
			Logger.Debug(Messages.BuildingEngine);
			var engine = this.Container.Resolve<IPersistStreams>();

			if (this.initialize)
			{
				Logger.Debug(Messages.InitializingEngine);
				engine.Initialize();
			}

			return base.Build();
		}
	}
}