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
		/// <returns>A stream of committed events for the aggregate indicated.</returns>
		CommittedEventStream Read(Guid id);

		/// <summary>
		/// Reads all events for the specified aggregate starting at the indicated version.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="startingVersion">The version at which the loaded event stream should begin.</param>
		/// <returns>A stream of committed events for the aggregate indicated.</returns>
		CommittedEventStream ReadFrom(Guid id, int startingVersion);

		/// <summary>
		/// Writes the stream of uncommitted events to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of uncomitted events to be persisted.</param>
		void Write(UncommittedEventStream stream);
	}
}