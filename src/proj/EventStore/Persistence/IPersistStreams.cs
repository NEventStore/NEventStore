namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IPersistStreams : IDisposable, ICommitEvents, IAccessSnapshots
	{
		/// <summary>
		/// Initializes and prepares the storage for use, if not already performed.
		/// </summary>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		void Initialize();

        /// <summary>        	
        /// Gets all commits on or after from the specified starting time.
        /// </summary>
        /// <param name="start">The point in time at which to start.</param>
        /// <returns>All commits that have occurred on or after the specified starting time.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<Commit> GetFrom(DateTime start);

		/// <summary>
		/// Gets a set of commits that has not yet been dispatched.
		/// </summary>
		/// <returns>The set of commits to be dispatched.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		IEnumerable<Commit> GetUndispatchedCommits();

		/// <summary>
		/// Marks the commit specified as dispatched.
		/// </summary>
		/// <param name="commit">The commit to be marked as dispatched.</param>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		void MarkCommitAsDispatched(Commit commit);

		/// <summary>
		/// Completely DESTROYS the contents of ANY and ALL streams that have been successfully persisted.  Use with caution.
		/// </summary>
		void Purge();
	}
}