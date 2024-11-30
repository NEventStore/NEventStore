#region

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Persistence;

#endregion

namespace NEventStore.Conversion;

public class EventUpconverterPipelineHook : PipelineHookBase
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
    private readonly IDictionary<Type, Func<object, object>> _converters;

    public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
    {
        _converters = converters ?? throw new ArgumentNullException(nameof(converters));
    }

    public override ICommit Select(ICommit committed)
    {
        var converted = false;
        var eventMessages = committed
            .Events
            .Select(eventMessage =>
            {
                var convert = Convert(eventMessage.Body);
                if (ReferenceEquals(convert, eventMessage.Body)) return eventMessage;
                converted = true;
                return new EventMessage { Headers = eventMessage.Headers, Body = convert };
            })
            .ToArray();
        if (!converted) return committed;
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
        if (!_converters.TryGetValue(source.GetType(), out var converter)) return source;

        var target = converter(source);
        Logger.LogDebug(Resources.ConvertingEvent, source.GetType(), target.GetType());

        return Convert(target);
    }
}