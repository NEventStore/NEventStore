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

        public override void Verbose(string message, params object[] values)
        {
            Log(ConsoleColor.DarkGreen, message, values);
        }

        public override void Debug(string message, params object[] values)
        {
            Log(ConsoleColor.Green, message, values);
        }

        public override void Info(string message, params object[] values)
        {
            Log(ConsoleColor.White, message, values);
        }

        public override void Warn(string message, params object[] values)
        {
            Log(ConsoleColor.Yellow, message, values);
        }

        public override void Error(string message, params object[] values)
        {
            Log(ConsoleColor.DarkRed, message, values);
        }

        public override void Fatal(string message, params object[] values)
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