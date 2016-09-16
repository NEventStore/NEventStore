using System;

namespace NEventStore.Logging
{
    /// <summary>
    ///     Indicates the ability to log diagnostic information.
    /// </summary>
    /// <remarks>
    ///     Object instances which implement this interface must be designed to be multi-thread safe.
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
        /// Is true if the logger has debug level enabled
        /// </summary>
        bool IsInfoEnabled { get; }

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

        public LogLevel LogLevel { get; private set; }


        public NEventStoreBaseLogger(LogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public bool IsVerboseEnabled
        {
            get
            {
                return LogLevel <= LogLevel.Verbose;
            }
        }

        public bool IsDebugEnabled
        {
            get
            {
                return LogLevel <= LogLevel.Debug;
            }
        }

        public bool IsInfoEnabled
        {
            get
            {
                return LogLevel <= LogLevel.Info;
            }
        }


        public void Verbose(string message, params object[] values)
        {
            if (IsVerboseEnabled) OnVerbose(message, values);
        }

        public void Debug(string message, params object[] values)
        {
            if (IsDebugEnabled) OnDebug(message, values);
        }

        public void Info(string message, params object[] values)
        {
            if (IsInfoEnabled) OnInfo(message, values);
        }

        public void Warn(string message, params object[] values)
        {
            OnWarn(message, values);
        }

        public void Error(string message, params object[] values)
        {
            OnError(message, values);
        }

        public void Fatal(string message, params object[] values)
        {
            OnFatal(message, values);
        }

        public abstract void OnDebug(string message, params object[] values);

        public abstract void OnError(string message, params object[] values);

        public abstract void OnFatal(string message, params object[] values);

        public abstract void OnInfo(string message, params object[] values);

        public abstract void OnVerbose(string message, params object[] values);

        public abstract void OnWarn(string message, params object[] values);

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