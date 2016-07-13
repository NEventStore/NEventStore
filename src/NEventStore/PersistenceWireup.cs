namespace NEventStore
{
    using System;
    using System.Transactions;
    using NEventStore.Diagnostics;
    using NEventStore.Logging;
    using NEventStore.Persistence;
    using NEventStore.Serialization;

    public class PersistenceWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (PersistenceWireup));
        private bool _initialize;
        private bool _tracking;
        private string _trackingInstanceName;

        public PersistenceWireup(Wireup inner)
            : base(inner)
        {
            Container.Register(TransactionScopeOption.Suppress);
        }

        public virtual PersistenceWireup WithPersistence(IPersistStreams instance)
        {
            Logger.Info(Messages.RegisteringPersistenceEngine, instance.GetType());
            With(instance);
            return this;
        }

        protected virtual SerializationWireup WithSerializer(ISerialize serializer)
        {
            return new SerializationWireup(this, serializer);
        }

        public virtual PersistenceWireup InitializeStorageEngine()
        {
            Logger.Info(Messages.ConfiguringEngineInitialization);
            _initialize = true;
            return this;
        }

        public virtual PersistenceWireup TrackPerformanceInstance(string instanceName)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException("instanceName", Messages.InstanceCannotBeNull);
            }

            Logger.Info(Messages.ConfiguringEnginePerformanceTracking);
            _tracking = true;
            _trackingInstanceName = instanceName;
            return this;
        }

        public virtual PersistenceWireup EnlistInAmbientTransaction()
        {
            Logger.Info(Messages.ConfiguringEngineEnlistment);
            Container.Register(TransactionScopeOption.Required);
            return this;
        }

        public override IStoreEvents Build()
        {
            Logger.Info(Messages.BuildingEngine);
            var engine = Container.Resolve<IPersistStreams>();

            if (_initialize)
            {
                Logger.Debug(Messages.InitializingEngine);
                engine.Initialize();
            }

            if (_tracking)
            {
                Container.Register<IPersistStreams>(new PerformanceCounterPersistenceEngine(engine, _trackingInstanceName));
            }

            return base.Build();
        }
    }
}