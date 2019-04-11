namespace NEventStore.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class EventUpconverterPipelineHook : PipelineHookBase
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
        private readonly IDictionary<Type, Func<object, object>> _converters;

        public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
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

        protected override void Dispose(bool disposing)
        {
            _converters.Clear();
            base.Dispose(disposing);
        }

        private object Convert(object source)
        {
            if (!_converters.TryGetValue(source.GetType(), out Func<object, object> converter))
            {
                return source;
            }

            object target = converter(source);
            if (Logger.IsDebugEnabled) Logger.Debug(Resources.ConvertingEvent, source.GetType(), target.GetType());

            return Convert(target);
        }
    }
}