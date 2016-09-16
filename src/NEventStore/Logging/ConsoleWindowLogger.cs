namespace NEventStore.Logging
{
    using System;

    public class ConsoleWindowLogger : NEventStoreBaseLogger
    {
        private static readonly object Sync = new object();
        private readonly ConsoleColor _originalColor = Console.ForegroundColor;
        private readonly Type _typeToLog;

        public int MyProperty { get; set; }

        public ConsoleWindowLogger(Type typeToLog, LogLevel logLevel = LogLevel.Info) : base (logLevel)
        {
            _typeToLog = typeToLog;
        }

        public override void OnVerbose(string message, params object[] values)
        {
            Log(ConsoleColor.DarkGreen, message, values);
        }

        public override void OnDebug(string message, params object[] values)
        {
            Log(ConsoleColor.Green, message, values);
        }

        public override void OnInfo(string message, params object[] values)
        {
            Log(ConsoleColor.White, message, values);
        }

        public override void OnWarn(string message, params object[] values)
        {
            Log(ConsoleColor.Yellow, message, values);
        }

        public override void OnError(string message, params object[] values)
        {
            Log(ConsoleColor.DarkRed, message, values);
        }

        public override void OnFatal(string message, params object[] values)
        {
            Log(ConsoleColor.Red, message, values);
        }

        private void Log(ConsoleColor color, string message, params object[] values)
        {
            lock (Sync)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message.FormatMessage(_typeToLog, values));
                Console.ForegroundColor = _originalColor;
            }
        }
    }
}