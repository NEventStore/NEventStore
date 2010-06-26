namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Represents a stream of events which has been already been committed to persistent storage.
	/// </summary>
	public class CommittedEventStream
	{
		/// <summary>
		/// Initializes a new instance of the CommittedEventStream class.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate to which the event stream belongs.</param>
		/// <param name="version">The version of the aggregate</param>
		/// <param name="events">The set of persisted events</param>
		/// <param name="snapshot">The most recent snapshot of the aggregate, if any.</param>
		public CommittedEventStream(Guid id, long version, IEnumerable events, object snapshot)
		{
			this.Id = id;
			this.Version = version;
			this.Events = events;
			this.Snapshot = snapshot;
		}

		/// <summary>
		/// Gets the value which uniquely identifies the aggregate to which the event stream belongs.
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// Gets the version of the aggregate.
		/// </summary>
		public long Version { get; private set; }

		/// <summary>
		/// Gets the set of persisted events.
		/// </summary>
		public IEnumerable Events { get; private set; }

		/// <summary>
		/// Gets the most recent snapshot of the aggregate, if any.
		/// </summary>
		public object Snapshot { get; private set; }
	}
}