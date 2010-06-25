namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// 
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		/// <param name="startingVersion"></param>
		/// <returns></returns>
		IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion);

		/// <summary>
		/// Saves all events provided to persistent storage.
		/// </summary>
		/// <param name="id">The value which uniquely identifies the aggregate being persisted.</param>
		/// <param name="aggregate">The type representing the aggregate being persisted.</param>
		/// <param name="events">The events to be committed.</param>
		/// <returns>Returns the version of the last event committed.</returns>
		int StoreEvents(Guid id, Type aggregate, IEnumerable events);

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		/// <returns></returns>
		T LoadSnapshot<T>(Guid id);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="version"></param>
		/// <param name="snapshot"></param>
		void StoreSnapshot(Guid id, int version, object snapshot);
	}
}