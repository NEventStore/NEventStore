namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Represents a stream of events which has not yet been committed to persistent storage.
	/// </summary>
	public class UncommittedEventStream
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
		/// Gets or sets the version of the stream of events when it was originally loaded.
		/// </summary>
		public long CommittedVersion { get; set; }

		/// <summary>
		/// Gets or sets the optional value which uniquely identifies the command for the events being persisted.
		/// </summary>
		public Guid CommandId { get; set; }

		/// <summary>
		/// Gets or sets the optional object which caused the uncommitted events, such as a command message.
		/// </summary>
		public object Command { get; set; }

		/// <summary>
		/// Gets or sets the collection of events to be persisted.
		/// </summary>
		public ICollection Events { get; set; }

		/// <summary>
		/// Gets or sets the optional snapshot of the aggregate.
		/// </summary>
		public object Snapshot { get; set; }
	}
}