namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Logging;

	public class EventUpconverterWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterWireup));
		private readonly List<Assembly> assembliesToScan = new List<Assembly>();

		public EventUpconverterWireup(Wireup wireup) : base(wireup)
		{
			Logger.Debug(Messages.EventUpconverterRegistered);

			this.Container.Register(c =>
			{
				if (!this.assembliesToScan.Any())
					this.assembliesToScan.AddRange(GetAllAssemblies());
				var converters = GetConverters(this.assembliesToScan);
				return new EventUpconverterPipelineHook(converters);
			});
		}

		private static Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
		{
			var c = from a in toScan
					from t in a.GetTypes()
					let i = t.GetInterface(typeof(IConvertEvents<,>).FullName)
					where i != null
					let sourceType = i.GetGenericArguments().First()
					let convertMethod = i.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
					let instance = Activator.CreateInstance(t)
					select new KeyValuePair<Type, Func<object, object>>(
						sourceType,
						e => convertMethod.Invoke(instance, new[] { e }));
			try
			{
				return c.ToDictionary(x => x.Key, x => x.Value);
			}
			catch (ArgumentException ex)
			{
				throw new MultipleConvertersFoundException(ex.Message, ex);
			}
		}

		private static IEnumerable<Assembly> GetAllAssemblies()
		{
			return Assembly.GetCallingAssembly()
				.GetReferencedAssemblies()
				.Select(Assembly.Load)
				.Concat(new[] { Assembly.GetCallingAssembly() });
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
	}
}
