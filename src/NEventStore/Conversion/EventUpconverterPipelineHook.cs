using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Persistence;

namespace NEventStore.Conversion
{
    /// <summary>
    /// Represents a pipeline hook that upconverts events.
    /// </summary>
    public class EventUpconverterPipelineHook : PipelineHookBase
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
        private readonly IDictionary<Type, Func<object, object>> _converters;

        /// <summary>
        /// Initializes a new instance of the EventUpconverterPipelineHook class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }

        /// <inheritdoc/>
        public override ICommit? SelectCommit(ICommit committed)
        {
            EventMessage[]? eventMessages = null;
            int index = 0;
            foreach (var eventMessage in committed.Events)
            {
                object convert = Convert(eventMessage.Body);
                if (!ReferenceEquals(convert, eventMessage.Body))
                {
                    // Most streams that flow through an upconverter do not necessarily contain
                    // events handled by the registered converters. Defer the output array until the
                    // first actual conversion so the no-conversion read path returns the original
                    // commit without allocating or copying every event message.
                    eventMessages ??= CopyEventMessages(committed.Events);
                    eventMessages[index] = new EventMessage { Headers = eventMessage.Headers, Body = convert };
                }

                index++;
            }

            if (eventMessages == null)
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

        private static EventMessage[] CopyEventMessages(ICollection<EventMessage> events)
        {
            var eventMessages = new EventMessage[events.Count];
            events.CopyTo(eventMessages, 0);
            return eventMessages;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _converters.Clear();
            base.Dispose(disposing);
        }

        private object Convert(object source)
        {
            Type sourceType = source.GetType();
            if (!_converters.TryGetValue(sourceType, out Func<object, object> converter))
            {
                return source;
            }

            object target = converter(source);
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.ConvertingEvent, sourceType, target.GetType());
            }

            return Convert(target);
        }
    }
}