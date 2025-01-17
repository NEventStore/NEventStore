namespace NEventStore.Persistence
{
    /// <summary>
    /// Asynchronous Interface: Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
    /// </summary>
    /// <remarks>
    /// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IPersistStreamsAsync : ICommitEventsAsync, IAccessSnapshotsAsync
    {
        /// <summary>
        /// Gets all commits (from all the buckets) after the specified checkpoint (excluded). Use 0 to get from the beginning.
        /// </summary>
        /// <param name="checkpointToken">The checkpoint token: all the commits after this one will be returned.</param>
        /// <param name="asyncObserver">The observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An enumerable of Commits.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        Task GetFromAsync(Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all commits (from all the buckets) after the specified checkpoint token (excluded) up to the specified end checkpoint token (included).
        /// </summary>
        /// <param name="fromCheckpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <param name="toCheckpointToken">The checkpoint token: all the commits tp to this one (included) will be returned</param>
        /// <param name="asyncObserver">The observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>All commits that have occurred on or after the specified checkpoint token up to the specified end checkpoint token.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        Task GetFromToAsync(Int64 fromCheckpointToken, Int64 toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all commits after the specified checkpoint (excluded) for a specific bucket. Use 0 to get from the beginning.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="checkpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <param name="asyncObserver">The observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An enumerable of Commits.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        Task GetFromAsync(string bucketId, Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all commits after the specified checkpoint token (excluded) up to the specified end checkpoint token (included).
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="fromCheckpointToken">The checkpoint token: all the commits after this one will be returned</param>
        /// <param name="toCheckpointToken">The checkpoint token: all the commits tp to this one (included) will be returned</param>
        /// <param name="asyncObserver">The observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>All commits that have occurred on or after the specified checkpoint token up to the specified end checkpoint token.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        Task GetFromToAsync(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completely DESTROYS the contents of ANY and ALL streams that have been successfully persisted.
        /// Use with caution.
        /// </summary>
        Task PurgeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Completely DESTROYS the contents of ANY and ALL streams that have been successfully persisted
        /// in the specified bucket.
        /// Use with caution.
        /// </summary>
        Task PurgeAsync(string bucketId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a stream.
        /// </summary>
        /// <param name="bucketId">The bucket Id from which the stream is to be deleted.</param>
        /// <param name="streamId">The stream Id of the stream that is to be deleted.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task DeleteStreamAsync(string bucketId, string streamId, CancellationToken cancellationToken = default);
    }
}