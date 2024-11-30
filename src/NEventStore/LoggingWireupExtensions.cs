#region

using System;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

#endregion

namespace NEventStore;

public static class LoggingWireupExtensions
{
    public static Wireup WithLoggerFactory(this Wireup wireup, ILoggerFactory loggerFactory)
    {
        return wireup.LogTo(type => loggerFactory.CreateLogger(type));
    }

    public static Wireup LogTo(this Wireup wireup, Func<Type, ILogger> logger)
    {
        LogFactory.BuildLogger = logger;
        return wireup;
    }
}