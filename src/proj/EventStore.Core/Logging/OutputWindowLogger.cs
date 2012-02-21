namespace EventStore.Logging
{
	using System;
	using System.Diagnostics;

	public class OutputWindowLogger : ILog
	{
		private static readonly object Sync = new object();
		private readonly Type typeToLog;

		public OutputWindowLogger(Type typeToLog)
		{
			this.typeToLog = typeToLog;
		}

		public virtual void Verbose(string message, params object[] values)
		{
			this.DebugWindow("Verbose", message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			this.DebugWindow("Debug", message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.TraceWindow("Info", message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.TraceWindow("Warn", message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.TraceWindow("Error", message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.TraceWindow("Fatal", message, values);
		}

		protected virtual void DebugWindow(string category, string message, params object[] values)
		{
			lock (Sync)
				System.Diagnostics.Debug.WriteLine(category, message.FormatMessage(this.typeToLog, values));
		}
		protected virtual void TraceWindow(string category, string message, params object[] values)
		{
			lock (Sync)
				Trace.WriteLine(category, message.FormatMessage(this.typeToLog, values));
		}
	}
}