namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Represents a stream of events which has been committed to persistent storage.
	/// </summary>
	public class CommittedEventStream
	{
		/// <summary>
		/// Initializes a new instance of the CommittedEventStream class.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate to which the event stream belongs.</param>
		/// <param name="version">The version of the aggregate</param>
		/// <param name="type">The type of aggregate to which the event stream belongs.</param>
		/// <param name="events">The collection of committed events</param>
		/// <param name="snapshot">The most recent snapshot of the aggregate, if any.</param>
		public CommittedEventStream(Guid id, long version, Type type, ICollection events, object snapshot)
		{
			this.Id = id;
			this.Type = type;
			this.Version = version;
			this.Events = events;
			this.Snapshot = snapshot;
		}

		/// <summary>
		/// Gets the value which uniquely identifies the aggregate to which the event stream belongs.
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// Gets the committed version of the aggregate.
		/// </summary>
		public long Version { get; private set; }

		/// <summary>
		/// Gets the type of aggregate to which the event stream belongs.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Gets the collection of committed events.
		/// </summary>
		public ICollection Events { get; private set; }

		/// <summary>
		/// Gets the most recent snapshot of the aggregate, if any.
		/// </summary>
		public object Snapshot { get; private set; }
	}
}