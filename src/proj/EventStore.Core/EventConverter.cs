using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EventStore
{
    public class EventConverter : IPipelineHook
    {
        private readonly Dictionary<Type, Func<object, object>> converters;

        public EventConverter()
        {
            var c = from a in AppDomain.CurrentDomain.GetAssemblies()
                    from t in a.GetTypes()
                    let i = t.GetInterface(typeof(IConvertEvents<,>).FullName)
                    where i != null
                    let sourceType = i.GetGenericArguments().First()
                    let convertMethod = t.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance)
                    let instance = Activator.CreateInstance(t)
                    select new KeyValuePair<Type, Func<object, object>>(
                        sourceType,
                        body => convertMethod.Invoke(instance, new[] { body as object })
                    );
            converters = c.ToDictionary(x => x.Key, x => x.Value);
        }

        private object Convert(object body)
        {
            Func<object, object> converter;
            if (this.converters.TryGetValue(body.GetType(), out converter))
                return converter(body);
            return body;

        }

        public void Dispose()
        {

        }

        public Commit Select(Commit committed)
        {
            foreach (var eventMessage in committed.Events)
            {
                eventMessage.Body = Convert(eventMessage.Body);
            }
            return committed;
        }

        public bool PreCommit(Commit attempt)
        {
            return true;
        }

        public void PostCommit(Commit committed)
        {
            // do noting
        }
    }
}
