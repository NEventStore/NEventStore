namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
	/// </summary>
	public interface IPersistStreams : IDisposable, ICommitEvents, IAccessSnapshots
	{
		/// <summary>
		/// Initializes and prepares the storage for use, if not already performed.
		/// </summary>
		/// <exception cref="StorageException" />
		void Initialize();

		/// <summary>
		/// Gets all commits on or after from the specified starting time.
		/// </summary>
		/// <param name="start">The point in time at which to start.</param>
		/// <returns>All commits that have occurred on or after the specified starting time.</returns>
		/// <exception cref="StorageException" />
		IEnumerable<Commit> GetFrom(DateTime start);

		/// <summary>
		/// Gets a set of commits that has not yet been dispatched.
		/// </summary>
		/// <returns>The set of commits to be dispatched.</returns>
		/// <exception cref="StorageException" />
		IEnumerable<Commit> GetUndispatchedCommits();

		/// <summary>
		/// Marks the commit specified as dispatched.
		/// </summary>
		/// <param name="commit">The commit to be marked as dispatched.</param>
		/// <exception cref="StorageException" />
		void MarkCommitAsDispatched(Commit commit);

		/// <summary>
		/// Gets identifiers for all streams whose head and last snapshot revisions differ by at least the threshold specified.
		/// </summary>
		/// <param name="maxThreshold">The maximum difference between the head and most recent snapshot revisions.</param>
		/// <returns>The streams for which the head and snapshot revisions differ by at least the threshold specified.</returns>
		/// <exception cref="StorageException" />
		IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold);

		/// <summary>
		/// Adds the snapshot provided to the stream indicated.
		/// </summary>
		/// <param name="snapshot">The snapshot to save.</param>
		/// <exception cref="StorageException" />
		void AddSnapshot(Snapshot snapshot);
	}
}