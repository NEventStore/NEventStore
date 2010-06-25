namespace EventStore
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The stream representing the set of events to be persisted.
	/// </summary>
	/// <typeparam name="TEvent">The class supertype or interface which all events implement.</typeparam>
	public class EventStream<TEvent>
	{
		/// <summary>
		/// Gets or sets the value which uniquely identifies the aggregate to which the event stream belongs.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the type of aggregate to which the event stream belongs.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Gets or sets the starting version of the event stream.
		/// </summary>
		public int Version { get; set; }

		/// <summary>
		/// Gets or sets the set of events to be persisted.
		/// </summary>
		public ICollection<TEvent> Events { get; set; }

		/// <summary>
		/// Gets or sets the snapshot of the aggregate, if any.
		/// </summary>
		public object Snapshot { get; set; }
	}
}