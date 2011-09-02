namespace EventStore
{
	using System;

	/// <summary>
	/// Provides the ability to log diagnostic messages to the configured hooks.
	/// </summary>
	public class Logger
	{
		/// <summary>
		/// Logs debug-level messages.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="values">Any optional values that help describe the message.</param>
		public static void Debug(string message, params object[] values)
		{
			DebugHook(message, values);
		}

		/// <summary>
		/// The action to be invoked when a debug-level message is reported.
		/// </summary>
		public static Action<string, object[]> DebugHook = (message, values) => { };

		/// <summary>
		/// Logs info-level messages.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="values">Any optional values that help describe the message.</param>
		public static void Info(string message, params object[] values)
		{
			InfoHook(message, values);
		}

		/// <summary>
		/// The action to be invoked when a info-level message is reported.
		/// </summary>
		public static Action<string, object[]> InfoHook = (message, values) => { };

		/// <summary>
		/// Logs warning-level messages.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="values">Any optional values that help describe the message.</param>
		public static void Warn(string message, params object[] values)
		{
			WarnHook(message, values);
		}

		/// <summary>
		/// The action to be invoked when a warning-level message is reported.
		/// </summary>
		public static Action<string, object[]> WarnHook = (message, values) => { };

		/// <summary>
		/// Logs error-level messages.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="values">Any optional values that help describe the message.</param>
		public static void Error(string message, params object[] values)
		{
			ErrorHook(message, values);
		}

		/// <summary>
		/// The action to be invoked when a error-level message is reported.
		/// </summary>
		public static Action<string, object[]> ErrorHook = (message, values) => { };

		/// <summary>
		/// Logs fatal-level messages.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="values">Any optional values that help describe the message.</param>
		public static void Fatal(string message, params object[] values)
		{
			FatalHook(message, values);
		}

		/// <summary>
		/// The action to be invoked when a fatal-level message is reported.
		/// </summary>
		public static Action<string, object[]> FatalHook = (message, values) => { };
	}
}