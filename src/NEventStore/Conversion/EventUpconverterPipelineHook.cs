namespace NEventStore.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class EventUpconverterPipelineHook : PipelineHookBase
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

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override ICommit Select(ICommit committed)
        {
            bool converted = false;
            var eventMessages = committed
                .Events
                .Select(eventMessage =>
                {
                    object convert = Convert(eventMessage.Body);
                    if (ReferenceEquals(convert, eventMessage.Body))
                    {
                        return eventMessage;
                    }
                    converted = true;
                    return new EventMessage { Headers = eventMessage.Headers, Body = convert };
                })
                .ToList();
            if (!converted)
            {
                return committed;
            }
            return new Commit(committed.BucketId,
                committed.StreamId,
                committed.StreamRevision,
                committed.CommitId,
                committed.CommitSequence,
                committed.CommitStamp,
                committed.CheckpointToken,
                committed.Headers,
                eventMessages);
        }

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