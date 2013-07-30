namespace NEventStore
{
    using System;
    using NEventStore.Logging;

    public static class LoggingWireupExtensions
    {
        public static Wireup LogToConsoleWindow(this Wireup wireup)
        {
            return wireup.LogTo(type => new ConsoleWindowLogger(type));
        }

        public static Wireup LogToOutputWindow(this Wireup wireup)
        {
            return wireup.LogTo(type => new OutputWindowLogger(type));
        }

        public static Wireup LogTo(this Wireup wireup, Func<Type, ILog> logger)
        {
            LogFactory.BuildLogger = logger;
            return wireup;
        }
    }
}