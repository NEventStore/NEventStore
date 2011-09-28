namespace EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;

    public class EventUpconverterEngine : IConvertCommits
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterEngine));
        private readonly Dictionary<Type, Func<object, object>> converters;

        public EventUpconverterEngine(IEnumerable<Assembly> assemblies)
        {
            var c = from a in assemblies
                    from t in a.GetTypes()
                    let i = t.GetInterface(typeof(IConvertEvents<,>).FullName)
                    where i != null
                    let sourceType = i.GetGenericArguments().First()
                    let convertMethod = i.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic )
                    let instance = Activator.CreateInstance(t)
                    select new KeyValuePair<Type, Func<object, object>>(
                        sourceType,
                        body => convertMethod.Invoke(instance, new[] { body as object })
                    );
            this.converters = c.ToDictionary(x => x.Key, x => x.Value);
        }

        private object Convert(object body)
        {
            Func<object, object> converter;
            object result = body;
            if (this.converters.TryGetValue(body.GetType(), out converter))
            {
                result = Convert(converter(body));
                Logger.Debug(Resources.ConvertingEvent, body.GetType(), result.GetType());
            }
            return result;
        }

        public Commit Convert(Commit committed)
        {
            foreach (var eventMessage in committed.Events)
            {
                eventMessage.Body = Convert(eventMessage.Body);
            }
            return committed;
        }

        public void Dispose()
        {
            this.converters.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
