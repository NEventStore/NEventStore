namespace EventStore.Logging
{
	using System;

	public class ConsoleWindowLogger : ILog
	{
		public static void MakePrimaryLogger()
		{
			LogFactory.BuildLogger = type => new ConsoleWindowLogger(type);
		}

		private static readonly object Sync = new object();
		private readonly Type typeToLog;
		private readonly ConsoleColor originalColor = Console.ForegroundColor;

		public ConsoleWindowLogger(Type typeToLog)
		{
			this.typeToLog = typeToLog;
		}

		public virtual void Verbose(string message, params object[] values)
		{
			this.Log(ConsoleColor.Green, message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			this.Log(ConsoleColor.Green, message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.Log(ConsoleColor.White, message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.Log(ConsoleColor.Yellow, message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.Log(ConsoleColor.Red, message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.Log(ConsoleColor.Red, message, values);
		}

		private void Log(ConsoleColor color, string message, params object[] values)
		{
			lock (Sync)
			{
				Console.ForegroundColor = color;
				Console.WriteLine(message.FormatMessage(this.typeToLog, values));
				Console.ForegroundColor = this.originalColor;
			}
		}
	}
}