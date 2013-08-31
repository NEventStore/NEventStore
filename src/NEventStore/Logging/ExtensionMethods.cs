namespace NEventStore.Logging
{
    using System;
    using System.Globalization;
    using System.Threading;

    internal static class ExtensionMethods
    {
        private const string MessageFormat = "{0:yyyy/MM/dd HH:mm:ss.ff} - {1} - {2} - {3}";

        public static string FormatMessage(this string message, Type typeToLog, params object[] values)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                MessageFormat,
                SystemTime.UtcNow,
                Thread.CurrentThread.GetName(),
                typeToLog.FullName,
                string.Format(CultureInfo.InvariantCulture, message, values));
        }

        private static string GetName(this Thread thread)
        {
            return !string.IsNullOrEmpty(thread.Name)
                ? thread.Name
                : thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
        }
    }
}