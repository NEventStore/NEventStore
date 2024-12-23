namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IPersistStreams : IDisposable, ICommitEvents, IAccessSnapshots
    {
        /// <summary>
        ///     Gets a value indicating whether this instance has been disposed of.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        ///     Initializes and prepares the storage for use, if not already performed.
        /// </summary>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        void Initialize();

        /// <summary>
        ///     Gets all commits on or after the specified starting time.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="startDate">The point in time at which to start.</param>
        /// <returns>All commits that have occurred on or after the specified starting time.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFrom(string bucketId, DateTime startDate);

        /// <summary>
        ///     Gets all commits on or after the specified starting time and before the specified end time.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="startDate">The point in time at which to start.</param>
        /// <param name="endDate">The point in time at which to end.</param>
        /// <returns>All commits that have occurred on or after the specified starting time and before the end time.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate);

        /// <summary>
        ///     Gets all commits (from all the buckets) after the specified checkpoint (excluded). Use 0 to get from the beginning.
        /// </summary>
        /// <param name="checkpointToken">The checkpoint token: all the commits after this one will be returned.</param>
        /// <returns>An enumerable of Commits.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFrom(Int64 checkpointToken);

        /// <summary>
        ///     Gets all commits (from all the buckets) after the specified checkpoint token (excluded) up to the specified end checkpoint token (included).
        /// </summary>
        /// <param name="fromCheckpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <param name="toCheckpointToken">The checkpoint token: all the commits tp to this one (included) will be returned</param>
        /// <returns>All commits that have occurred on or after the specified checkpoint token up to the specified end checkpoint token.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken);

        /// <summary>
        ///     Gets all commits after the specified checkpoint (excluded) for a specific bucket. Use 0 to get from the beginning.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="checkpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <returns>An enumerable of Commits.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken);

        /// <summary>
        ///     Gets all commits after the specified checkpoint token (excluded) up to the specified end checkpoint token (included).
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="fromCheckpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <param name="toCheckpointToken">The checkpoint token: all the commits tp to this one (included) will be returned</param>
        /// <returns>All commits that have occurred on or after the specified checkpoint token up to the specified end checkpoint token.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken);

        /// <summary>
        ///     Completely DESTROYS the contents of ANY and ALL streams that have been successfully persisted.
        ///     Use with caution.
        /// </summary>
        void Purge();

        /// <summary>
        ///     Completely DESTROYS the contents of ANY and ALL streams that have been successfully persisted
        ///     in the specified bucket.
        ///     Use with caution.
        /// </summary>
        void Purge(string bucketId);

        /// <summary>
        ///     Completely DESTROYS the contents and schema (if applicable) containing ANY and ALL streams that have been
        ///     successfully persisted.
        ///     Use with caution.
        /// </summary>
        void Drop();

        /// <summary>
        /// Deletes a stream.
        /// </summary>
        /// <param name="bucketId">The bucket Id from which the stream is to be deleted.</param>
        /// <param name="streamId">The stream Id of the stream that is to be deleted.</param>
        void DeleteStream(string bucketId, string streamId);
    }
}