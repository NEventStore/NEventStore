namespace EventStore
{
	using System;
	using System.Collections.Generic;

	public interface IStoreEvents
	{
		IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion);
		int StoreEvents<T>(Guid id, Type aggregate, IEnumerable<T> events);

		T LoadSnapshot<T>(Guid id);
		void StoreSnapshot<T>(Guid id, int version, T snapshot);
	}
}