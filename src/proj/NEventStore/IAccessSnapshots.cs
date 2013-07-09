namespace EventStore
{
	using System;
	using System.Collections.Generic;
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

		/// <summary>
		/// Gets identifiers for all streams whose head and last snapshot revisions differ by at least the threshold specified.
		/// </summary>
		/// <param name="maxThreshold">The maximum difference between the head and most recent snapshot revisions.</param>
		/// <returns>The streams for which the head and snapshot revisions differ by at least the threshold specified.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold);
	}
}