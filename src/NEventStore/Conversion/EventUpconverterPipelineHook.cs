namespace NEventStore.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;
    using NEventStore.Persistence;

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
        /// <param name="converters"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }

        /// <inheritdoc/>
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
                .ToArray();
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

        /// <inheritdoc/>
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
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.ConvertingEvent, source.GetType(), target.GetType());
            }

            return Convert(target);
        }
    }
}