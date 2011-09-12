namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Logging;

	/// <summary>
	/// A set of common methods used through the EventStore.
	/// </summary>
	public static class ExtensionMethods
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ExtensionMethods));

		/// <summary>
		/// Enumerates the set of items provided and then calls the complete action(s) specified.
		/// </summary>
		/// <typeparam name="T">The type of items to be enumerated</typeparam>
		/// <param name="items">The items to be enumerated.</param>
		/// <param name="complete">The action(s) to be invoked upon successful enumeration of the items provided.</param>
		/// <returns>All items contained within the set of items provided.</returns>
		public static IEnumerable<T> Yield<T>(this IEnumerable<T> items, params Action[] complete)
		{
			Logger.Verbose("Yielding (paging) through result set.");
			foreach (var item in items ?? new T[0])
				yield return item;

			Logger.Verbose("Yield completed, invoking completion actions.");
			foreach (var item in (complete ?? new Action[0]).Where(x => x != null))
				item();
		}

		/// <summary>
		/// Formats the string provided using the values specified.
		/// </summary>
		/// <param name="format">The string to be formated.</param>
		/// <param name="values">The values to be embedded into the string.</param>
		/// <returns>The formatted string.</returns>
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
		}
	}
}