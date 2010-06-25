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
		/// Gets or sets the version of the aggregate.
		/// </summary>
		public long Version { get; set; }

		/// <summary>
		/// Gets or sets the collection of persisted events.
		/// </summary>
		public IEnumerable Events { get; set; }

		/// <summary>
		/// Gets or sets the snapshot of the aggregate, if any.
		/// </summary>
		public object Snapshot { get; set; }
	}
}