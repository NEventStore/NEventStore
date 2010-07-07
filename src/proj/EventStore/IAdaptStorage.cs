namespace EventStore
{
	using System;
	using System.Collections;

	/// <summary>
	/// Adapts the underlying persistence infrastructure to facilitate event sourcing.
	/// </summary>
	public interface IAdaptStorage
	{
		/// <summary>
		/// Reads all events for the specified aggregate.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="maxStartingVersion">The maxium version at which to start reading the aggregate event stream.</param>
		/// <returns>A stream of committed events for the specified aggregate.</returns>
		/// <exception cref="StorageEngineException" />
		CommittedEventStream LoadById(Guid id, long maxStartingVersion);

		/// <summary>
		/// Reads all events associated with the specified command identifier.
		/// </summary>
		/// <param name="commandId">The value which uniquely identifies the set of events to be loaded.</param>
		/// <returns>A collection of committed events for the specified command identifier.</returns>
		/// <exception cref="StorageEngineException" />
		ICollection LoadByCommandId(Guid commandId);

		/// <summary>
		/// Reads all events for the specified aggregate starting after the indicated version.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="version">The version after which the loaded event collection should begin.</param>
		/// <returns>A collection of committed events for the specified aggregate.</returns>
		/// <exception cref="StorageEngineException" />
		ICollection LoadStartingAfter(Guid id, long version);

		/// <summary>
		/// Writes the stream of uncommitted events to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of uncomitted events to be persisted.</param>
		/// <exception cref="ConcurrencyException" />
		/// <exception cref="DuplicateCommandException" />
		/// <exception cref="StorageEngineException" />
		void Save(UncommittedEventStream stream);
	}
}