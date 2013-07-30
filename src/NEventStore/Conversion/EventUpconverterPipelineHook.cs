namespace NEventStore.Conversion
{
    using System;
    using System.Collections.Generic;
    using NEventStore.Logging;

    public class EventUpconverterPipelineHook : IPipelineHook
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (EventUpconverterPipelineHook));
        private readonly IDictionary<Type, Func<object, object>> _converters;

        public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
        {
            if (converters == null)
            {
                throw new ArgumentNullException("converters");
            }

            _converters = converters;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual Commit Select(Commit committed)
        {
            foreach (var eventMessage in committed.Events)
            {
                eventMessage.Body = Convert(eventMessage.Body);
            }

            return committed;
        }

        public virtual bool PreCommit(Commit attempt)
        {
            return true;
        }

        public virtual void PostCommit(Commit committed)
        {}

        protected virtual void Dispose(bool disposing)
        {
            _converters.Clear();
        }

        private object Convert(object source)
        {
            Func<object, object> converter;
            if (!_converters.TryGetValue(source.GetType(), out converter))
            {
                return source;
            }

            object target = converter(source);
            Logger.Debug(Resources.ConvertingEvent, source.GetType(), target.GetType());

            return Convert(target);
        }
    }
}