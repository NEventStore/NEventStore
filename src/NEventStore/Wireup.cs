namespace NEventStore;

using System.Collections.Generic;
using System.Linq;
using Conversion;
using Persistence;
using Persistence.InMemory;
using Logging;
using Microsoft.Extensions.Logging;

public class Wireup
{
    private readonly NanoContainer _container;
    private readonly Wireup _inner;
    private readonly ILogger Logger = LogFactory.BuildLogger(typeof(Wireup));

    protected Wireup(NanoContainer container)
    {
        _container = container;
    }

    protected Wireup(Wireup inner)
    {
        _inner = inner;
    }

    protected NanoContainer Container => _container ?? _inner.Container;

    public static Wireup Init()
    {
        var container = new NanoContainer();

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
        Logger.LogInformation(Resources.WireupHookIntoPipeline, string.Join(", ", hooks.Select(h => h.GetType())));
        ICollection<IPipelineHook> collection = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
        Container.Register(collection);
        return this;
    }

    public virtual IStoreEvents Build()
    {
        if (_inner != null) return _inner.Build();

        return Container.Resolve<IStoreEvents>();
    }

    /// <summary>
    /// <para>
    /// Provide some additionl concurrency checks to avoid useless roundtrips to the databases in a non-transactional environment.
    /// </para>
    /// <para>
    /// If you enable any sort of two-phase commit and/or transactional behavior on the Persistence drivers
    /// you should not use the <see cref="OptimisticPipelineHook"/> module.
    /// </para>
    /// </summary>
    /// <param name="maxStreamsToTrack"></param>
    /// <returns></returns>
    public Wireup UseOptimisticPipelineHook(int maxStreamsToTrack = OptimisticPipelineHook.MaxStreamsToTrack)
    {
        Container.Register(_ => new OptimisticPipelineHook(maxStreamsToTrack));
        return this;
    }

    private static IStoreEvents BuildEventStore(NanoContainer context)
    {
        var concurrency = context.Resolve<OptimisticPipelineHook>();
        var upconverter = context.Resolve<EventUpconverterPipelineHook>();

        var hooks = context.Resolve<ICollection<IPipelineHook>>() ?? new IPipelineHook[0];
        hooks = new IPipelineHook[] { concurrency, upconverter }
            .Concat(hooks)
            .Where(x => x != null)
            .ToArray();

        return new OptimisticEventStore(context.Resolve<IPersistStreams>(), hooks);
    }
}