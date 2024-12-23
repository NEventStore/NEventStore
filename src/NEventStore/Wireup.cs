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
        private readonly NanoContainer _container;
        private readonly Wireup _inner;
        private readonly ILogger Logger = LogFactory.BuildLogger(typeof(Wireup));

        /// <summary>
        /// Initializes a new instance of the Wireup class.
        /// </summary>
        /// <param name="container"></param>
        protected Wireup(NanoContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Initializes a new instance of the Wireup class.
        /// </summary>
        /// <param name="inner"></param>
        protected Wireup(Wireup inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets the container.
        /// </summary>
        protected NanoContainer Container
        {
            get { return _container ?? _inner.Container; }
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
        public virtual Wireup With<T>(T instance) where T : class
        {
            Container.Register(instance);
            return this;
        }

        /// <summary>
        /// Add a pipeline hook to the processing pipeline.
        /// </summary>
        public virtual Wireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
        {
            return HookIntoPipelineUsing((hooks ?? Array.Empty<IPipelineHook>()).ToArray());
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
            ICollection<IPipelineHook> collection = (hooks ?? Array.Empty<IPipelineHook>()).Where(x => x != null).ToArray();
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

            return Container.Resolve<IStoreEvents>();
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

            ICollection<IPipelineHook> hooks = context.Resolve<ICollection<IPipelineHook>>() ?? Array.Empty<IPipelineHook>();
            hooks = new IPipelineHook[] { concurrency, upconverter }
                .Concat(hooks)
                .Where(x => x != null)
                .ToArray();

            return new OptimisticEventStore(context.Resolve<IPersistStreams>(), hooks);
        }
    }
}