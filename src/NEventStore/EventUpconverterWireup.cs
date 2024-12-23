using System.Reflection;
using Microsoft.Extensions.Logging;
using NEventStore.Conversion;
using NEventStore.Logging;

namespace NEventStore
{
    /// <summary>
    ///    Represents the configuration for event upconverters.
    /// </summary>
    public class EventUpconverterWireup : Wireup
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(EventUpconverterWireup));
        private readonly List<Assembly> _assembliesToScan = new List<Assembly>();
        private readonly Dictionary<Type, Func<object, object>> _registered = new Dictionary<Type, Func<object, object>>();

        /// <summary>
        /// Initializes a new instance of the EventUpconverterWireup class.
        /// </summary>
        /// <param name="wireup"></param>
        public EventUpconverterWireup(Wireup wireup) : base(wireup)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.EventUpconverterRegistered);
            }

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
                if (converters.Count > 0)
                {
                    return new EventUpconverterPipelineHook(converters);
                }
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

        private static Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
        {
            IEnumerable<KeyValuePair<Type, Func<object, object>>> c = from a in toScan
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

        /// <summary>
        /// Scans the specified assemblies for event upconverters.
        /// </summary>
        public virtual EventUpconverterWireup WithConvertersFrom(params Assembly[] assemblies)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.EventUpconvertersLoadedFrom,
                    string.Join(", ", assemblies.Select(a => $"{a.GetName().Name} {a.GetName().Version}")));
            }
            _assembliesToScan.AddRange(assemblies);
            return this;
        }

        /// <summary>
        /// Scans the assemblies containing the converters for event upconverters.
        /// </summary>
        public virtual EventUpconverterWireup WithConvertersFromAssemblyContaining(params Type[] converters)
        {
            IEnumerable<Assembly> assemblies = converters.Select(c => c.Assembly).Distinct();
            return this.WithConvertersFrom(assemblies.ToArray());
        }

        /// <summary>
        /// Adds a converter to the pipeline.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual EventUpconverterWireup AddConverter<TSource, TTarget>(
            IUpconvertEvents<TSource, TTarget> converter)
            where TSource : class
            where TTarget : class
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _registered[typeof(TSource)] = @event => converter.Convert(@event as TSource);

            return this;
        }
    }
}