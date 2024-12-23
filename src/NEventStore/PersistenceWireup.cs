using Microsoft.Extensions.Logging;
using NEventStore.Diagnostics;
using NEventStore.Logging;
using NEventStore.Persistence;
using NEventStore.Serialization;

namespace NEventStore
{
    /// <summary>
    ///    Represents the persistence wireup.
    /// </summary>
    public class PersistenceWireup : Wireup
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));
        private bool _initialize;
#if NET462
        private bool _tracking;
        private string _trackingInstanceName;
#endif

        /// <summary>
        /// Initializes a new instance of the PersistenceWireup class.
        /// </summary>
        public PersistenceWireup(Wireup inner)
            : base(inner)
        {
#pragma warning disable S125 // Sections of code should not be commented out
            /* EnlistInAmbientTransaction: Will be moved to the specific Persistence driver or completely removed letting the clients handle that
            Container.Register(TransactionScopeOption.Suppress);
            */
#pragma warning restore S125 // Sections of code should not be commented out
        }

        /// <summary>
        /// Configures the persistence engine to use the specified serializer.
        /// </summary>
        public virtual PersistenceWireup WithPersistence(IPersistStreams instance)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.RegisteringPersistenceEngine, instance.GetType());
            }
            With(instance);
            return this;
        }

        /// <summary>
        /// Configures the persistence engine to use the specified serializer.
        /// </summary>
        protected virtual SerializationWireup WithSerializer(ISerialize serializer)
        {
            return new SerializationWireup(this, serializer);
        }

        /// <summary>
        /// Initializes the storage engine.
        /// </summary>
        public virtual PersistenceWireup InitializeStorageEngine()
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.ConfiguringEngineInitialization);
            }
            _initialize = true;
            return this;
        }

#if NET462
        /// <summary>
        /// Configures the persistence engine to track performance counters.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual PersistenceWireup TrackPerformanceInstance(string instanceName)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName), Messages.InstanceCannotBeNull);
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.ConfiguringEnginePerformanceTracking);
            }
            _tracking = true;
            _trackingInstanceName = instanceName;
            return this;
        }
#endif

#pragma warning disable S125 // Sections of code should not be commented out
        /* EnlistInAmbientTransaction: Will be moved to the specific Persistence driver or completely removed letting the clients handle that
                /// <summary>
                /// Enables two-phase commit.
                /// By default NEventStore will suppress surrounding TransactionScopes 
                /// (All the Persistence drivers that support transactions will create a 
                /// private nested TransactionScope with <see cref="TransactionScopeOption.Suppress"/> for each operation)
                /// so that all of its operations run in a dedicated, separate transaction.
                /// This option changes the behavior so that NEventStore enlists in a surrounding TransactionScope,
                /// if there is any (All the Persistence drivers that support transactions will create a 
                /// private nested TransactionScope with <see cref="TransactionScopeOption.Required"/> for each operation).
                /// </summary>
                /// <remarks>
                /// Enabling the two-phase commit will also disable the <see cref="OptimisticPipelineHook"/>
                /// that provide some additionl concurrency checks to avoid useless roundtrips to the databases.
                /// </remarks>
                /// <returns></returns>
                public virtual PersistenceWireup EnlistInAmbientTransaction()
                {
                    if (Logger.IsInfoEnabled) Logger.Info(Messages.ConfiguringEngineEnlistment);
                    Container.Register(TransactionScopeOption.Required);
                    return this;
                }
        */
#pragma warning restore S125 // Sections of code should not be commented out

        /// <summary>
        /// Builds the persistence engine.
        /// </summary>
        public override IStoreEvents Build()
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.BuildingEngine);
            }

            var engine = Container.Resolve<IPersistStreams>();

            if (_initialize)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug(Messages.InitializingEngine);
                }
                engine.Initialize();
            }

#if NET462
            if (_tracking)
            {
                Container.Register<IPersistStreams>(new PerformanceCounterPersistenceEngine(engine, _trackingInstanceName));
            }
#endif
            return base.Build();
        }
    }
}