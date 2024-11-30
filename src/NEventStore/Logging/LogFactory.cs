namespace NEventStore.Logging;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
///     Provides the ability to get a new instance of the configured logger.
/// </summary>
public static class LogFactory
{
    /// <summary>
    ///     Initializes static members of the LogFactory class.
    /// </summary>
    static LogFactory()
    {
        var logger = NullLogger.Instance;
        BuildLogger = type => logger;
    }

    /// <summary>
    ///     Gets or sets the log builder of the configured logger.
    ///     This should be invoked to return a new logging instance.
    /// </summary>
    public static Func<Type, ILogger> BuildLogger { get; set; }
}