namespace EventStore
{
	using System;
	using Persistence;

	/// <summary>
	/// Indicates the ability to get or retrieve a snapshot for a given stream.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IAccessSnapshots
	{
		/// <summary>
		/// Gets the most recent snapshot which was taken on or before the revision indicated.
		/// </summary>
		/// <param name="streamId">The stream to be searched for a snapshot.</param>
		/// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
		/// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		Snapshot GetSnapshot(Guid streamId, int maxRevision);

		/// <summary>
		/// Adds the snapshot provided to the stream indicated.
		/// </summary>
		/// <param name="snapshot">The snapshot to save.</param>
		/// <returns>If the snapshot was added, returns true; otherwise false.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		bool AddSnapshot(Snapshot snapshot);
	}
}