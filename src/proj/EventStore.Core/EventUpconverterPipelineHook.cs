namespace EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;

    public class EventUpconverterPipelineHook : IPipelineHook
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
        private readonly Dictionary<Type, Func<object, object>> converters;

        public EventUpconverterPipelineHook(Dictionary<Type, Func<object, object>> converters)
        {
            this.converters = converters;
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
        }

        public void Dispose()
        {
            this.converters.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
