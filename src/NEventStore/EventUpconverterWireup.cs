#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NEventStore.Conversion;
using NEventStore.Logging;

#endregion

namespace NEventStore;

public class EventUpconverterWireup : Wireup
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(EventUpconverterWireup));
    private readonly List<Assembly> _assembliesToScan = new();

    private readonly IDictionary<Type, Func<object, object>> _registered =
        new Dictionary<Type, Func<object, object>>();

    public EventUpconverterWireup(Wireup wireup) : base(wireup)
    {
        Logger.LogDebug(Messages.EventUpconverterRegistered);

        Container.Register(_ =>
        {
            if (_registered.Count > 0) return new EventUpconverterPipelineHook(_registered);

            if (_assembliesToScan.Count == 0) _assembliesToScan.AddRange(GetAllAssemblies());

            var converters = GetConverters(_assembliesToScan);
            if (converters.Count > 0) return new EventUpconverterPipelineHook(converters);
            return null;
        });
    }

    private static IEnumerable<Assembly> GetAllAssemblies()
    {
        return Assembly.GetCallingAssembly()
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Concat(new[] { Assembly.GetCallingAssembly() });
    }

    private static IDictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
    {
        var c = from a in toScan
            from t in a.GetTypes()
            where !t.IsAbstract
            let i = t.GetInterface(typeof(IUpconvertEvents<,>).FullName)
            where i != null
            let sourceType = i.GetGenericArguments().First()
            let convertMethod = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
            let instance = Activator.CreateInstance(t)
            select new KeyValuePair<Type, Func<object, object>>(
                sourceType, e => convertMethod.Invoke(instance, new[] { e }));
        try
        {
            return c.ToDictionary(x => x.Key, x => x.Value);
        }
        catch (ArgumentException e)
        {
            throw new MultipleConvertersFoundException(e.Message, e);
        }
    }

    public virtual EventUpconverterWireup WithConvertersFrom(params Assembly[] assemblies)
    {
        Logger.LogDebug(Messages.EventUpconvertersLoadedFrom,
            string.Join(", ", assemblies.Select(a => $"{a.GetName().Name} {a.GetName().Version}")));
        _assembliesToScan.AddRange(assemblies);
        return this;
    }

    public virtual EventUpconverterWireup WithConvertersFromAssemblyContaining(params Type[] converters)
    {
        var assemblies = converters.Select(c => c.Assembly).Distinct();
        return WithConvertersFrom(assemblies.ToArray());
    }

    public virtual EventUpconverterWireup AddConverter<TSource, TTarget>(
        IUpconvertEvents<TSource, TTarget> converter)
        where TSource : class
        where TTarget : class
    {
        if (converter == null) throw new ArgumentNullException(nameof(converter));

        _registered[typeof(TSource)] = @event => converter.Convert(@event as TSource);

        return this;
    }
}