namespace EventStore
{
    using System;
    using System.Transactions;
    using Diagnostics;
    using Logging;
    using Persistence;
    using Serialization;

    public class PersistenceWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));
		private bool initialize;
		private bool tracking;
		private string trackingInstanceName;

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
		public virtual PersistenceWireup TrackPerformanceInstance(string instanceName)
		{
			if (instanceName == null)
				throw new ArgumentNullException("instanceName", Messages.InstanceCannotBeNull);

			Logger.Debug(Messages.ConfiguringEnginePerformanceTracking);
			this.tracking = true;
			this.trackingInstanceName = instanceName;
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

			if (this.tracking)
				this.Container.Register<IPersistStreams>(new PerformanceCounterPersistenceEngine(engine, this.trackingInstanceName));

			return base.Build();
		}
	}
}