namespace EventStore
{
	using System;

	/// <summary>
	/// Provides the ability to save and retrieve objects which utilize event sourcing.
	/// </summary>
	/// <typeparam name="T">The class supertype or interface which all events implement..</typeparam>
	public interface IStoreEvents<T>
	{
		/// <summary>
		/// Reads all events for the specified aggregate.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <returns>A stream of events for the aggregate indicated.</returns>
		EventStream<T> Read(Guid id);

		/// <summary>
		/// Reads all events for the specified aggregate starting at the indicated version.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate of the events to be loaded.</param>
		/// <param name="startingVersion">The version at which the loaded event stream should begin.</param>
		/// <returns>A stream of events for the aggregate indicated.</returns>
		EventStream<T> ReadFrom(Guid id, int startingVersion);

		/// <summary>
		/// Writes the event stream to persistent storage.
		/// </summary>
		/// <param name="stream">The stream of events to be persisted.</param>
		void Write(EventStream<T> stream);
	}
}