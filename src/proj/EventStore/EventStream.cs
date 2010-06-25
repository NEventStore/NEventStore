namespace EventStore
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The stream representing the set of events to be persisted.
	/// </summary>
	/// <typeparam name="T">The class supertype or interface which all events implement.</typeparam>
	public class EventStream<T>
	{
		/// <summary>
		/// Gets or sets the value which uniquely identifies the aggregate to which the event stream belongs.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the starting version of the event stream.
		/// </summary>
		public int Version { get; set; }

		/// <summary>
		/// Gets or sets the type of aggregate to which the event stream belongs.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Gets or sets the set of events to be persisted.
		/// </summary>
		public IEnumerable<T> Events { get; set; }
	}
}