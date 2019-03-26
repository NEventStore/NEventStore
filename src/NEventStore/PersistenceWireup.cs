namespace NEventStore
{
    using System;
#if !NETSTANDARD1_6
    using System.Transactions;
#endif
    using NEventStore.Diagnostics;
    using NEventStore.Logging;
    using NEventStore.Persistence;
    using NEventStore.Serialization;

    public class PersistenceWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));
        private bool _initialize;
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
        private bool _tracking;
        private string _trackingInstanceName;
#endif

        public PersistenceWireup(Wireup inner)
            : base(inner)
        {
#if !NETSTANDARD1_6
            Container.Register(TransactionScopeOption.Suppress);
#endif
        }

        public virtual PersistenceWireup WithPersistence(IPersistStreams instance)
        {
            if (Logger.IsInfoEnabled) Logger.Info(Messages.RegisteringPersistenceEngine, instance.GetType());
            With(instance);
            return this;
        }

        protected virtual SerializationWireup WithSerializer(ISerialize serializer)
        {
            return new SerializationWireup(this, serializer);
        }

        public virtual PersistenceWireup InitializeStorageEngine()
        {
            if (Logger.IsInfoEnabled) Logger.Info(Messages.ConfiguringEngineInitialization);
            _initialize = true;
            return this;
        }

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
        public virtual PersistenceWireup TrackPerformanceInstance(string instanceName)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException("instanceName", Messages.InstanceCannotBeNull);
            }

            if (Logger.IsInfoEnabled) Logger.Info(Messages.ConfiguringEnginePerformanceTracking);
            _tracking = true;
            _trackingInstanceName = instanceName;
            return this;
        }
#endif

#if !NETSTANDARD1_6
        public virtual PersistenceWireup EnlistInAmbientTransaction()
        {
            if (Logger.IsInfoEnabled) Logger.Info(Messages.ConfiguringEngineEnlistment);
            Container.Register(TransactionScopeOption.Required);
            return this;
        }
#endif

        public override IStoreEvents Build()
        {
            if (Logger.IsInfoEnabled) Logger.Info(Messages.BuildingEngine);

            var engine = Container.Resolve<IPersistStreams>();

            if (_initialize)
            {
                if (Logger.IsDebugEnabled) Logger.Debug(Messages.InitializingEngine);
                engine.Initialize();
            }

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
            if (_tracking)
            {
                Container.Register<IPersistStreams>(new PerformanceCounterPersistenceEngine(engine, _trackingInstanceName));
            }
#endif
            return base.Build();
        }
    }
}