namespace EventStore
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;
	using Dispatcher;
	using Persistence;
	using Persistence.InMemoryPersistence;

	public class Wireup
	{
		private readonly Wireup inner;
		private readonly NanoContainer container;

		protected Wireup(NanoContainer container)
		{
			this.container = container;
		}
		protected Wireup(Wireup inner)
		{
			this.inner = inner;
		}

		public static Wireup Init()
		{
			var container = new NanoContainer();

			container.Register(TransactionScopeOption.Suppress);
			container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
			container.Register(BuildEventStore);

			return new Wireup(container);
		}

		protected NanoContainer Container
		{
			get { return this.container ?? this.inner.Container; }
		}

		public virtual Wireup With<T>(T instance) where T : class
		{
			this.Container.Register(instance);
			return this;
		}

		public virtual Wireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
		{
			return this.HookIntoPipelineUsing((hooks ?? new IPipelineHook[0]).ToArray());
		}
		public virtual Wireup HookIntoPipelineUsing(params IPipelineHook[] hooks)
		{
			ICollection<IPipelineHook> collection = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
			this.Container.Register(collection);
			return this;
		}

		public virtual IStoreEvents Build()
		{
			if (this.inner != null)
				return this.inner.Build();

			return this.Container.Resolve<IStoreEvents>();
		}

		private static IStoreEvents BuildEventStore(NanoContainer context)
		{
			var scopeOption = context.Resolve<TransactionScopeOption>();
			var concurrencyHook = scopeOption == TransactionScopeOption.Suppress ? new OptimisticPipelineHook() : null;
			var dispatchSchedulerHook = new DispatchSchedulerPipelinkHook(context.Resolve<IScheduleDispatches>());
            var eventUpconverterPipelineHook = context.Resolve<EventUpconverterPipelineHook>();

			var pipelineHooks = context.Resolve<ICollection<IPipelineHook>>() ?? new IPipelineHook[0];
            pipelineHooks = new IPipelineHook[] {
                    eventUpconverterPipelineHook, 
                    concurrencyHook, 
                    dispatchSchedulerHook
                }
                .Concat(pipelineHooks)
                .Where(x => x != null)
                .ToArray();

			return new OptimisticEventStore(context.Resolve<IPersistStreams>(), pipelineHooks);
		}
	}
}