namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Conversion;
	using Logging;

	public class EventUpconverterWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterWireup));
		private readonly IDictionary<Type, Func<object, object>> registered = new Dictionary<Type, Func<object, object>>();
		private readonly List<Assembly> assembliesToScan = new List<Assembly>();

		public EventUpconverterWireup(Wireup wireup) : base(wireup)
		{
			Logger.Debug(Messages.EventUpconverterRegistered);

			this.Container.Register(c =>
			{
				if (this.registered.Count > 0)
					return new EventUpconverterPipelineHook(this.registered);

				if (!this.assembliesToScan.Any())
					this.assembliesToScan.AddRange(GetAllAssemblies());

				var converters = GetConverters(this.assembliesToScan);
				return new EventUpconverterPipelineHook(converters);
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
			Logger.Debug(Messages.EventUpconvertersLoadedFrom, string.Concat(", ", assemblies));
			this.assembliesToScan.AddRange(assemblies);
			return this;
		}
		public virtual EventUpconverterWireup WithConvertersFromAssemblyContaining(params Type[] converters)
		{
			var assemblies = converters.Select(c => c.Assembly).Distinct();
			Logger.Debug(Messages.EventUpconvertersLoadedFrom, string.Concat(", ", assemblies));
			this.assembliesToScan.AddRange(assemblies);
			return this;
		}

		public virtual EventUpconverterWireup AddConverter<TSource, TTarget>(
			IUpconvertEvents<TSource, TTarget> converter)
			where TSource : class
			where TTarget : class
		{
			if (converter == null)
				throw new ArgumentNullException("converter");

			this.registered[typeof(TSource)] = @event => converter.Convert(@event as TSource);

			return this;
		}
	}
}