namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Provides the ability to load and save objects which utilize event sourcing.
	/// </summary>
	public interface IStorageEngine
	{
		/// <summary>
		/// Reads all events for the specified aggregate.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <returns>A stream of committed events for the specified aggregate.</returns>
		CommittedEventStream LoadById(Guid id);

		/// <summary>
		/// Reads all events associated with the specified command identifier.
		/// </summary>
		/// <param name="commandId">The value which uniquely identifies the set of events to be loaded.</param>
		/// <returns>A collection of committed events for the specified command identifier.</returns>
		ICollection LoadByCommandId(Guid commandId);

		/// <summary>
		/// Reads all events for the specified aggregate starting after the indicated version.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="version">The version after which the loaded event collection should begin.</param>
		/// <returns>A collection of committed events for the specified aggregate.</returns>
		ICollection LoadStartingAfter(Guid id, long version);

		/// <summary>
		/// Writes the stream of uncommitted events to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of uncomitted events to be persisted.</param>
		/// <param name="initialVersion">The version when the aggregate was loaded.</param>
		void Save(UncommittedEventStream stream, long initialVersion);
	}
}