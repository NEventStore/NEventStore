namespace NEventStore.Logging
{
    /// <summary>
    ///     Indicates the ability to log diagnostic information.
    /// </summary>
    /// <remarks>
    ///     Object instances which implement this interface must be designed to be multi-thread safe.
    ///     The logging class is intended to be very simple and fast, so all the checks to see if the
    ///     desired logging level is supported should be done in code.
    ///     This logging class is not a fully featured logging framework and is intended to be used 
    ///     only within the NEventStore project.
    /// </remarks>
    public interface ILog
    {
        /// <summary>
        /// Is true if the logger has verbose level enabled
        /// </summary>
        bool IsVerboseEnabled { get; }

        /// <summary>
        /// Is true if the logger has debug level enabled
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Is true if the logger has info level enabled
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Is true if the logger has warn level enabled
        /// </summary>
        bool IsWarnEnabled { get; }

        /// <summary>
        /// Is true if the logger has error level enabled
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Is true if the logger has fatal level enabled
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Level of the logger
        /// </summary>
        LogLevel LogLevel { get; }

        /// <summary>
        ///     Logs the most detailed level of diagnostic information.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Verbose(string message, params object[] values);

        /// <summary>
        ///     Logs the debug-level diagnostic information.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Debug(string message, params object[] values);

        /// <summary>
        ///     Logs important runtime diagnostic information.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Info(string message, params object[] values);

        /// <summary>
        ///     Logs diagnostic issues to which attention should be paid.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Warn(string message, params object[] values);

        /// <summary>
        ///     Logs application and infrastructure-level errors.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Error(string message, params object[] values);

        /// <summary>
        ///     Logs fatal errors which result in process termination.
        /// </summary>
        /// <param name="message">The diagnostic message to be logged.</param>
        /// <param name="values">All parameter to be formatted into the message, if any.</param>
        void Fatal(string message, params object[] values);
    }

    public abstract class NEventStoreBaseLogger : ILog
    {
        private LogLevel _logLevel;

        public LogLevel LogLevel
        {
            get => _logLevel;
            private set
            {
                _logLevel = value;
                IsVerboseEnabled = _logLevel <= LogLevel.Verbose;
                IsDebugEnabled = _logLevel <= LogLevel.Debug;
                IsInfoEnabled = _logLevel <= LogLevel.Info;
                IsWarnEnabled = _logLevel <= LogLevel.Warn;
                IsErrorEnabled = _logLevel <= LogLevel.Error;
                IsFatalEnabled = _logLevel <= LogLevel.Fatal;
            }
        }

        public NEventStoreBaseLogger(LogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public bool IsVerboseEnabled { get; private set; }

        public bool IsDebugEnabled { get; private set; }

        public bool IsInfoEnabled { get; private set; }

        public bool IsWarnEnabled { get; private set; }

        public bool IsErrorEnabled { get; private set; }

        public bool IsFatalEnabled { get; private set; }

        public abstract void Debug(string message, params object[] values);

        public abstract void Error(string message, params object[] values);

        public abstract void Fatal(string message, params object[] values);

        public abstract void Info(string message, params object[] values);

        public abstract void Verbose(string message, params object[] values);

        public abstract void Warn(string message, params object[] values);
    }

    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }
}