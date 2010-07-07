namespace EventStore
{
	using System;

	/// <summary>
	/// Represents the mechanism used to read from and write to persistent event storage.
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// Reads all events for the specified aggregate.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="maxStartingVersion">The maximum version at which to start reading the aggregate event stream.</param>
		/// <returns>A stream of committed events for the specified aggregate.</returns>
		CommittedEventStream Read(Guid id, long maxStartingVersion);

		/// <summary>
		/// Writes the stream of uncommitted events to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of uncomitted events to be persisted.</param>
		void Write(UncommittedEventStream stream);
	}
}