using NEventStore.Conversion;
using NEventStore.Persistence;
using NEventStore.Persistence.InMemory;
using NEventStore.Logging;
using Microsoft.Extensions.Logging;

namespace NEventStore
{
    /// <summary>
    /// Represents the configuration for the event store.
    /// </summary>
    public class Wireup
    {
        private readonly NanoContainer? _container;
        private readonly Wireup? _inner;
        private readonly ILogger Logger = LogFactory.BuildLogger(typeof(Wireup));

        /// <summary>
        /// Initializes a new instance of the Wireup class.
        /// </summary>
        protected Wireup(NanoContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Initializes a new instance of the Wireup class.
        /// </summary>
        protected Wireup(Wireup inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the container.
        /// </summary>
        protected NanoContainer Container
        {
            get { return _container ?? _inner!.Container; }
        }

        /// <summary>
        /// Initializes a new instance of the Wire-up class.
        /// </summary>
        public static Wireup Init()
        {
            var container = new NanoContainer();

            container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
            container.Register(BuildEventStore);

            return new Wireup(container);
        }

        /// <summary>
        /// Registers a service with the container.
        /// </summary>
        public virtual Wireup Register<T>(T instance) where T : class
        {
            Container.Register(instance);
            return this;
        }

        /// <summary>
        /// Add a pipeline hook to the processing pipeline.
        /// </summary>
        public virtual Wireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
        {
            return HookIntoPipelineUsing((hooks ?? []).ToArray());
        }

        /// <summary>
        /// Add pipeline hooks to the processing pipeline.
        /// </summary>
        public virtual Wireup HookIntoPipelineUsing(params IPipelineHook[] hooks)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupHookIntoPipeline, string.Join(", ", hooks.Select(h => h.GetType())));
            }
            ICollection<IPipelineHook> collection = (hooks ?? []).Where(x => x != null).ToArray();
            Container.Register(collection);
            return this;
        }

        /// <summary>
        /// <para>Add pipeline hooks to the processing pipeline.</para>
        /// <para>Asynchronous hooks are executed after the synchronous hooks.</para>
        /// </summary>
        public virtual Wireup HookIntoPipelineUsing(params IPipelineHookAsync[] hooks)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.WireupHookIntoPipeline, string.Join(", ", hooks.Select(h => h.GetType())));
            }
            ICollection<IPipelineHookAsync> collection = (hooks ?? []).Where(x => x != null).ToArray();
            Container.Register(collection);
            return this;
        }

        /// <summary>
        /// Builds the configured event store.
        /// </summary>
        public virtual IStoreEvents Build()
        {
            if (_inner != null)
            {
                return _inner.Build();
            }

            return Container.Resolve<IStoreEvents>()
                ?? throw new InvalidOperationException("IStoreEvents was not registered.");
        }

        /// <summary>
        /// <para>
        /// Provide some additional concurrency checks to avoid useless roundtrips to the databases in a non-transactional environment.
        /// </para>
        /// <para>
        /// If you enable any sort of two-phase commit and/or transactional behavior on the Persistence drivers
        /// you should not use the <see cref="OptimisticPipelineHook"/> module.
        /// </para>
        /// </summary>
        public Wireup UseOptimisticPipelineHook(int maxStreamsToTrack = OptimisticPipelineHook.MaxStreamsToTrack)
        {
            Container.Register(_ => new OptimisticPipelineHook(maxStreamsToTrack));
            return this;
        }

        private static IStoreEvents BuildEventStore(NanoContainer context)
        {
            var concurrency = context.Resolve<OptimisticPipelineHook>();
            var upconverter = context.Resolve<EventUpconverterPipelineHook>();

            ICollection<IPipelineHook> hooks = context.Resolve<ICollection<IPipelineHook>>() ?? [];
            var pipelineHooks = new List<IPipelineHook>(hooks);
            if (concurrency != null)
            {
                pipelineHooks.Add(concurrency);
            }
            if (upconverter != null)
            {
                pipelineHooks.Add(upconverter);
            }
            ICollection<IPipelineHookAsync> hooksAsync = context.Resolve<ICollection<IPipelineHookAsync>>() ?? [];
            return new OptimisticEventStore(context.Resolve<IPersistStreams>()!, pipelineHooks, hooksAsync);
        }
    }
}