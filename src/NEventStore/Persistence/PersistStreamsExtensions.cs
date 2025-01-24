namespace NEventStore.Persistence
{
    /// <summary>
    /// Provides a set of extension methods for the <see cref="IPersistStreams"/> interface.
    /// </summary>
    public static class PersistStreamsExtensions
    {
        /// <summary>
        /// Deletes a stream from the default bucket.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="streamId">The stream id to be deleted.</param>
        public static void DeleteStream(this IPersistStreams persistStreams, string streamId)
        {
            persistStreams.DeleteStream(Bucket.Default, streamId);
        }

        /// <summary>
        /// Deletes a stream from the default bucket.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="streamId">The stream id to be deleted.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static Task DeleteStreamAsync(this IPersistStreams persistStreams, string streamId, CancellationToken cancellationToken = default)
        {
            return persistStreams.DeleteStreamAsync(Bucket.Default, streamId, cancellationToken);
        }

        /// <summary>
        /// Returns a single commit from any bucket.
        /// Wrapper for the <see cref="IPersistStreamsSync.GetFromTo(long, long)"/> function in order to
        /// return a single commit.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="checkpointToken">The checkpoint token that mark the commit to read.</param>
        /// <returns>A single commit.</returns>
        public static ICommit GetCommit(this IPersistStreams persistStreams, Int64 checkpointToken)
        {
            return persistStreams.GetFromTo(checkpointToken - 1, checkpointToken).SingleOrDefault();
        }

        /// <summary>
        /// Returns a single commit from any bucket.
        /// Wrapper for the <see cref="IPersistStreamsSync.GetFromTo(long, long)"/> function in order to
        /// return a single commit.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="checkpointToken">The checkpoint token that mark the commit to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A single commit.</returns>
        public static async Task<ICommit> GetCommitAsync(this IPersistStreams persistStreams, Int64 checkpointToken, CancellationToken cancellationToken = default)
        {
            var observer = new CommitStreamObserver();
            await persistStreams.GetFromToAsync(checkpointToken - 1, checkpointToken, observer, cancellationToken).ConfigureAwait(false);
            return observer.Commits.SingleOrDefault();
        }
    }
}