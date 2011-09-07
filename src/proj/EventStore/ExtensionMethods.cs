namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A set of common methods used through the EventStore.
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Enumerates the set of items provided and then calls the complete action(s) specified.
		/// </summary>
		/// <typeparam name="T">The type of items to be enumerated</typeparam>
		/// <param name="items">The items to be enumerated.</param>
		/// <param name="complete">The action(s) to be invoked upon successful enumeration of the items provided.</param>
		/// <returns>All items contained within the set of items provided.</returns>
		public static IEnumerable<T> Yield<T>(this IEnumerable<T> items, params Action[] complete)
		{
			foreach (var item in items ?? new T[0])
				yield return item;

			foreach (var item in (complete ?? new Action[0]).Where(x => x != null))
				item();
		}
	}
}