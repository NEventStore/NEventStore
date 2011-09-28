using System;
using System.Collections.Generic;
using System.Linq;
namespace EventStore
{
    using System.Reflection;
    using Logging;

    public class EventUpconverterWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterWireup));
        private readonly List<Assembly> assembliesToScan = new List<Assembly>();

        public EventUpconverterWireup(Wireup wireup) : base(wireup)
        {
            Logger.Debug(Messages.EventUpconverterRegistered);

            this.Container.Register<IConvertCommits>(c =>
            {
                if (!assembliesToScan.Any())
                    assembliesToScan.AddRange(getAllAssemblies());
                return new EventUpconverterEngine(assembliesToScan);
            });
        }

        private IEnumerable<Assembly> getAllAssemblies()
        {
            return Assembly.GetCallingAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] {Assembly.GetCallingAssembly()});
        }

        public virtual EventUpconverterWireup UsingConvertersFrom(params Assembly[] assemblies)
        {
            Logger.Debug(Messages.EventUpconvertersLoadedFrom, string.Concat(", ", assemblies));
            this.assembliesToScan.AddRange(assemblies);
            return this;
        }

        public virtual EventUpconverterWireup UsingConvertersFromAssemblyContaining(params Type[] converters)
        {
            var assemblies = converters.Select(c => c.Assembly)
                .Distinct();
            Logger.Debug(Messages.EventUpconvertersLoadedFrom, string.Concat(", ", assemblies));
            this.assembliesToScan.AddRange(assemblies);
            return this;
        }
    }
}
