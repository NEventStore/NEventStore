using NEventStore.Persistence;

namespace NEventStore
{
    /// <summary>
    ///    Provides extension methods for <see cref="IAccessSnapshots"/> and <see cref="IAccessSnapshotsAsync"/>.
    /// </summary>
    public static class AccessSnapshotsExtensions
    {
        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated from the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshots"/> instance.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static ISnapshot GetSnapshot(this IAccessSnapshots accessSnapshots, Guid streamId, int maxRevision)
        {
            return GetSnapshot(accessSnapshots, streamId.ToString(), maxRevision);
        }

        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated from the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshots"/> instance.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static ISnapshot GetSnapshot(this IAccessSnapshots accessSnapshots, string streamId, int maxRevision)
        {
            return accessSnapshots.GetSnapshot(Bucket.Default, streamId, maxRevision);
        }

        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshots"/> instance.</param>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static ISnapshot GetSnapshot(this IAccessSnapshots accessSnapshots, string bucketId, Guid streamId, int maxRevision)
        {
            return accessSnapshots.GetSnapshot(bucketId, streamId.ToString(), maxRevision);
        }

        /// <summary>
        ///     Gets identifiers for all streams whose head and last snapshot revisions differ by at least the threshold specified for the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshots"/> instance.</param>
        /// <param name="maxThreshold">The maximum difference between the head and most recent snapshot revisions.</param>
        /// <returns>The streams for which the head and snapshot revisions differ by at least the threshold specified.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static IEnumerable<IStreamHead> GetStreamsToSnapshot(this IAccessSnapshots accessSnapshots, int maxThreshold)
        {
            return accessSnapshots.GetStreamsToSnapshot(Bucket.Default, maxThreshold);
        }

        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated from the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshotsAsync"/> instance.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static Task<ISnapshot> GetSnapshotAsync(this IAccessSnapshotsAsync accessSnapshots, Guid streamId, int maxRevision, CancellationToken cancellationToken)
        {
            return GetSnapshotAsync(accessSnapshots, streamId.ToString(), maxRevision, cancellationToken);
        }

        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated from the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshotsAsync"/> instance.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static Task<ISnapshot> GetSnapshotAsync(this IAccessSnapshotsAsync accessSnapshots, string streamId, int maxRevision, CancellationToken cancellationToken)
        {
            return accessSnapshots.GetSnapshotAsync(Bucket.Default, streamId, maxRevision, cancellationToken);
        }

        /// <summary>
        ///     Gets the most recent snapshot which was taken on or before the revision indicated.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshotsAsync"/> instance.</param>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The stream to be searched for a snapshot.</param>
        /// <param name="maxRevision">The maximum revision possible for the desired snapshot.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>If found, it returns the snapshot; otherwise null is returned.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static Task<ISnapshot> GetSnapshotAsync(this IAccessSnapshotsAsync accessSnapshots, string bucketId, Guid streamId, int maxRevision, CancellationToken cancellationToken)
        {
            return accessSnapshots.GetSnapshotAsync(bucketId, streamId.ToString(), maxRevision, cancellationToken);
        }

        /// <summary>
        ///     Gets identifiers for all streams whose head and last snapshot revisions differ by at least the threshold specified for the default bucket.
        /// </summary>
        /// <param name="accessSnapshots">The <see cref="IAccessSnapshotsAsync"/> instance.</param>
        /// <param name="maxThreshold">The maximum difference between the head and most recent snapshot revisions.</param>
        /// <param name="asyncObserver">The observer to receive the streams.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        public static Task GetStreamsToSnapshotAsync(this IAccessSnapshotsAsync accessSnapshots, int maxThreshold, IAsyncObserver<IStreamHead> asyncObserver, CancellationToken cancellationToken)
        {
            return accessSnapshots.GetStreamsToSnapshotAsync(Bucket.Default, maxThreshold, asyncObserver, cancellationToken);
        }
    }
}