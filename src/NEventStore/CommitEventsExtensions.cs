using NEventStore.Persistence;

namespace NEventStore
{
    /// <summary>
    ///    Provides extension methods for <see cref="ICommitEvents"/> instances.
    /// </summary>
    public static class CommitEventsExtensions
    {
        /// <summary>
        ///     Gets the corresponding commits from the stream indicated starting at the revision specified until the
        ///     end of the stream sorted in ascending order--from oldest to newest from the default bucket.
        /// </summary>
        /// <param name="commitEvents">The <see cref="ICommitEvents"/> instance.</param>
        /// <param name="streamId">The stream from which the events will be read.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <returns>A series of committed events from the stream specified sorted in ascending order.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static IEnumerable<ICommit> GetFrom(this ICommitEvents commitEvents, string streamId, int minRevision, int maxRevision)
        {
            if (commitEvents == null)
            {
                throw new ArgumentNullException(nameof(commitEvents));
            }

            return commitEvents.GetFrom(Bucket.Default, streamId, minRevision, maxRevision);
        }

        /// <summary>
        ///     Gets the corresponding commits from the stream indicated starting at the revision specified until the
        ///     end of the stream sorted in ascending order--from oldest to newest from the default bucket.
        /// </summary>
        /// <param name="commitEvents">The <see cref="ICommitEvents"/> instance.</param>
        /// <param name="streamId">The stream from which the events will be read.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <param name="observer">Observer to receive the commits.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static Task GetFromAsync(this ICommitEventsAsync commitEvents, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            if (commitEvents == null)
            {
                throw new ArgumentNullException(nameof(commitEvents));
            }

            return commitEvents.GetFromAsync(Bucket.Default, streamId, minRevision, maxRevision, observer, cancellationToken);
        }
    }
}