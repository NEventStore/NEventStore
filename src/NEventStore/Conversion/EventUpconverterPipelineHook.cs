namespace NEventStore.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
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

        public virtual ICommit Select(ICommit committed)
        {
            var eventMessages = committed
                .Events
                .Select(eventMessage => new EventMessage {Headers = eventMessage.Headers, Body = Convert(eventMessage.Body)})
                .Cast<IEventMessage>()
                .ToList();

            return new Commit(committed.BucketId,
                committed.StreamId,
                committed.StreamRevision,
                committed.CommitId,
                committed.CommitSequence,
                committed.CommitStamp,
                committed.Headers,
                eventMessages);
        }

        public virtual bool PreCommit(ICommit attempt)
        {
            return true;
        }

        public virtual void PostCommit(ICommit committed)
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