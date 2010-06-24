namespace EventStore
{
	using System;
	using System.Collections.Generic;

	public interface IStoreEvents
	{
		IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion) where T : class;
		int StoreEvents<T>(Guid id, IEnumerable<T> events) where T : class;

		T LoadSnapshot<T>(Guid id) where T : class;
		void StoreSnapshot<T>(Guid id, int version, T snapshot) where T : class;
	}
}