namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NEventStore.Conversion;
    using NEventStore.Logging;

    public class EventUpconverterWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (EventUpconverterWireup));
        private readonly List<Assembly> _assembliesToScan = new List<Assembly>();
        private readonly IDictionary<Type, Func<object, object>> _registered = new Dictionary<Type, Func<object, object>>();

        public EventUpconverterWireup(Wireup wireup) : base(wireup)
        {
            if (Logger.IsDebugEnabled) Logger.Debug(Messages.EventUpconverterRegistered);

            Container.Register(_ =>
            {
                if (_registered.Count > 0)
                {
                    return new EventUpconverterPipelineHook(_registered);
                }

                if (_assembliesToScan.Count == 0)
                {
                    _assembliesToScan.AddRange(GetAllAssemblies());
                }

                IDictionary<Type, Func<object, object>> converters = GetConverters(_assembliesToScan);
                return new EventUpconverterPipelineHook(converters);
            });
        }

        private static IEnumerable<Assembly> GetAllAssemblies()
        {
#if !NETSTANDARD1_6
            return Assembly.GetCallingAssembly()
                           .GetReferencedAssemblies()
                           .Select(Assembly.Load)
                           .Concat(new[] {Assembly.GetCallingAssembly()});
#else
            // in netstandard1.6 we return an empty assembly array instead of looking at all the assemblies in the folder
            // GetCallingAssembly is not supported
            return new Assembly[] { };
#endif
        }

        private static IDictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
        {
#if !NETSTANDARD1_6
            IEnumerable<KeyValuePair<Type, Func<object, object>>> c = from a in toScan
                                                                      from t in a.GetTypes()
                                                                      where !t.IsAbstract
                                                                      let i = t.GetInterface(typeof (IUpconvertEvents<,>).FullName)
                                                                      where i != null
                                                                      let sourceType = i.GetGenericArguments().First()
                                                                      let convertMethod = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
                                                                      let instance = Activator.CreateInstance(t)
                                                                      select new KeyValuePair<Type, Func<object, object>>(
                                                                          sourceType, e => convertMethod.Invoke(instance, new[] {e}));
#else
            IEnumerable<KeyValuePair<Type, Func<object, object>>> c = from a in toScan
                                                                      from t in a.GetTypes()
                                                                      where !t.GetTypeInfo().IsAbstract
                                                                      let i = t.GetTypeInfo().GetInterface(typeof (IUpconvertEvents<,>).FullName)
                                                                      where i != null
                                                                      let sourceType = i.GetTypeInfo().GetGenericArguments().First()
                                                                      let convertMethod = i.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
                                                                      let instance = Activator.CreateInstance(t)
                                                                      select new KeyValuePair<Type, Func<object, object>>(
                                                                          sourceType, e => convertMethod.Invoke(instance, new[] {e}));
#endif
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
            if (Logger.IsDebugEnabled)
            {
                Logger.Debug(Messages.EventUpconvertersLoadedFrom,
                    string.Join(", ", assemblies.Select(a => $"{a.GetName().Name} {a.GetName().Version}")));
            }
            _assembliesToScan.AddRange(assemblies);
            
            return this;
        }

        public virtual EventUpconverterWireup WithConvertersFromAssemblyContaining(params Type[] converters)
        {
#if !NETSTANDARD1_6
            IEnumerable<Assembly> assemblies = converters.Select(c => c.Assembly).Distinct();
#else
            IEnumerable<Assembly> assemblies = converters.Select(c => c.GetTypeInfo().Assembly).Distinct();
#endif
            return this.WithConvertersFrom(assemblies.ToArray());
        }

        public virtual EventUpconverterWireup AddConverter<TSource, TTarget>(
            IUpconvertEvents<TSource, TTarget> converter)
            where TSource : class
            where TTarget : class
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _registered[typeof (TSource)] = @event => converter.Convert(@event as TSource);

            return this;
        }
    }
}