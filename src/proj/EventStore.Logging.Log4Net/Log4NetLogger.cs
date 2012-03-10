namespace EventStore.Logging.Log4Net
{
	using System;

	public class Log4NetLogger : ILog
	{
		private readonly log4net.ILog log;

		public Log4NetLogger(Type typeToLog)
		{
			this.log = log4net.LogManager.GetLogger(typeToLog);
		}

		public virtual void Verbose(string message, params object[] values)
		{
			if (this.log.IsDebugEnabled)
				this.log.DebugFormat(message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			if (this.log.IsDebugEnabled)
				this.log.DebugFormat(message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			if (this.log.IsInfoEnabled)
				this.log.InfoFormat(message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			if (this.log.IsWarnEnabled)
				this.log.WarnFormat(message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			if (this.log.IsErrorEnabled)
				this.log.ErrorFormat(message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			if (this.log.IsFatalEnabled)
				this.log.FatalFormat(message, values);
		}
	}
}