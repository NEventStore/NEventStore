using NEventStore.Persistence;

namespace NEventStore
{
    /// <summary>
    /// Indicates the ability to commit events and access events to and from a given stream.
    /// </summary>
    /// <remarks>
    /// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface ICommitEventsAsync
    {
        /// <summary>
        /// Gets the corresponding commits (possibly using an async cursor) from the stream indicated
        /// starting at the revision specified until the end of the stream sorted
        /// in ascending order--from oldest to newest.
        /// Each commit will be passed to an observer.
        /// Reading operation will stop:
        /// - when the maxRevision is reached.
        /// - if CancellationToken is set.
        /// - when there are no more commits to read.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The stream from which the events will be read.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <param name="observer">Observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the to-be-committed events provided to the underlying persistence mechanism.
        /// </summary>
        /// <param name="attempt">The series of events and associated metadata to be committed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="ConcurrencyException" />
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        /// <remarks>
        /// This interface returns a nullable ICommit object because it's implemented by <see cref="OptimisticEventStore"/>
        /// that can return null if the pipeline hooks decide to abort the commit.
        /// </remarks>
        Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken = default);
    }
}