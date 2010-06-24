namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public interface IStoreEvents
	{
		IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion);
		int StoreEvents(Guid id, Type aggregate, IEnumerable events);

		T LoadSnapshot<T>(Guid id);
		void StoreSnapshot(Guid id, int version, object snapshot);
	}
}