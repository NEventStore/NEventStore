namespace NEventStore
{
    using System.Collections.Generic;
    using System.Linq;
#if !NETSTANDARD1_6
    using System.Transactions;
#endif
    using NEventStore.Conversion;
    using NEventStore.Persistence;
    using NEventStore.Persistence.InMemory;
    using NEventStore.Serialization;
    using Logging;

    public class Wireup
    {
        private readonly NanoContainer _container;
        private readonly Wireup _inner;
        private ILog Logger = LogFactory.BuildLogger(typeof(Wireup));

        protected Wireup(NanoContainer container)
        {
            _container = container;
        }

        protected Wireup(Wireup inner)
        {
            _inner = inner;
        }

        protected NanoContainer Container
        {
            get { return _container ?? _inner.Container; }
        }

        public static Wireup Init()
        {
            var container = new NanoContainer();
#if !NETSTANDARD1_6
            container.Register(TransactionScopeOption.Suppress);
#endif
            container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
            container.Register(BuildEventStore);

            return new Wireup(container);
        }

        public virtual Wireup With<T>(T instance) where T : class
        {
            Container.Register(instance);
            return this;
        }

        public virtual Wireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
        {
            return HookIntoPipelineUsing((hooks ?? new IPipelineHook[0]).ToArray());
        }

        public virtual Wireup HookIntoPipelineUsing(params IPipelineHook[] hooks)
        {
            Logger.Info(Resources.WireupHookIntoPipeline, string.Join(", ", hooks.Select(h => h.GetType())));
            ICollection<IPipelineHook> collection = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
            Container.Register(collection);
            return this;
        }

        public virtual IStoreEvents Build()
        {
            if (_inner != null)
            {
                return _inner.Build();
            }

            return Container.Resolve<IStoreEvents>();
        }

        private static IStoreEvents BuildEventStore(NanoContainer context)
        {
#if !NETSTANDARD1_6
            var scopeOption = context.Resolve<TransactionScopeOption>();
            OptimisticPipelineHook concurrency = scopeOption == TransactionScopeOption.Suppress ? new OptimisticPipelineHook() : null;
#else
            OptimisticPipelineHook concurrency = new OptimisticPipelineHook();
#endif
            var upconverter = context.Resolve<EventUpconverterPipelineHook>();

            ICollection<IPipelineHook> hooks = context.Resolve<ICollection<IPipelineHook>>() ?? new IPipelineHook[0];
            hooks = new IPipelineHook[] { concurrency, upconverter }
                .Concat(hooks)
                .Where(x => x != null)
                .ToArray();

            return new OptimisticEventStore(context.Resolve<IPersistStreams>(), hooks);
        }
    }
}