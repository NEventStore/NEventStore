namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Represents a stream of events that have been already been committed to persistent storage.
	/// </summary>
	public class CommittedEventStream
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
		public ICollection Events { get; set; }

		/// <summary>
		/// Gets or sets the snapshot of the aggregate, if any.
		/// </summary>
		public object Snapshot { get; set; }
	}
}