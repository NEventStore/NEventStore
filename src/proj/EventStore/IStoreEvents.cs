namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to save and retrieve objects which utilize event sourcing.
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// Loads all events for the specified aggregate starting at the indicated version.
		/// </summary>
		/// <typeparam name="T">The common type which all events implement.</typeparam>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="startingVersion">The version at which the loaded event stream should begin.</param>
		/// <returns>A set of events for the aggregate indicated.</returns>
		IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion);

		/// <summary>
		/// Saves all events provided to persistent storage.
		/// </summary>
		/// <typeparam name="T">The class supertype or interface which all events implement.</typeparam>
		/// <param name="stream">The stream of events to be persisted.</param>
		void StoreEvents<T>(EventStream<T> stream);

		/// <summary>
		/// Loads the snapshot for the aggregate specified.
		/// </summary>
		/// <typeparam name="T">The type of snapshot being loaded.</typeparam>
		/// <param name="id">The value which uniquely identifies the snapshot to be loaded.</param>
		/// <returns>The snapshot for the identifier specified.</returns>
		T LoadSnapshot<T>(Guid id);

		/// <summary>
		/// Saves the snapshot for a particular aggregate.
		/// </summary>
		/// <param name="snapshot">The snapshot to be persisted.</param>
		void StoreSnapshot(SnapshotPayload snapshot);
	}
}