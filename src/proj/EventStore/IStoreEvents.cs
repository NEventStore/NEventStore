namespace EventStore
{
	using System;

	/// <summary>
	/// Provides the ability to save and retrieve objects which utilize event sourcing.
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// Reads all events for the specified aggregate.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <returns>A stream of committed events for the specified aggregate.</returns>
		CommittedEventStream Read(Guid id);

		/// <summary>
		/// Reads all events for the specified aggregate starting at the indicated version.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="version">The version at which the loaded event stream should begin.</param>
		/// <returns>A stream of committed events for the specified aggregate.</returns>
		CommittedEventStream ReadStartingFrom(Guid id, long version);

		/// <summary>
		/// Reads all events associated with the specified correlation identifier.
		/// </summary>
		/// <param name="correlationId">The value which uniquely identifies the set of events to be loaded.</param>
		/// <returns>A stream of committed events for the specified correlation identifier.</returns>
		CommittedEventStream ReadByCorrelationId(Guid correlationId);

		/// <summary>
		/// Writes the stream of uncommitted events to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of uncomitted events to be persisted.</param>
		void Write(UncommittedEventStream stream);
	}
}