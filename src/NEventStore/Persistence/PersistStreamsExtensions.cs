namespace NEventStore.Persistence
{
    /// <summary>
    ///    Provides a set of extension methods for the <see cref="IPersistStreams"/> interface.
    /// </summary>
    public static class PersistStreamsExtensions
    {
        /// <summary>
        ///     Gets all commits on or after from the specified starting time from the default bucket.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="start">The point in time at which to start.</param>
        /// <returns>All commits that have occurred on or after the specified starting time.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        [Obsolete("Will be removed in a future revision because of inconsistency with GetFrom(checkpointToken) which returns commits from all the buckets")]
        public static IEnumerable<ICommit> GetFrom(this IPersistStreams persistStreams, DateTime start)
        {
            if (persistStreams == null)
            {
                throw new ArgumentNullException(nameof(persistStreams));
            }
            return persistStreams.GetFrom(Bucket.Default, start);
        }

        /// <summary>
        ///     Gets all commits on or after from the specified starting time and before the specified end time from the default bucket.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="start">The point in time at which to start.</param>
        /// <param name="end">The point in time at which to end.</param>
        /// <returns>All commits that have occurred on or after the specified starting time and before the end time.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        [Obsolete("Will be removed in a future revision because of inconsistency with GetFrom(checkpointToken, from, to) which returns commits from all the buckets")]
        public static IEnumerable<ICommit> GetFromTo(this IPersistStreams persistStreams, DateTime start, DateTime end)
        {
            if (persistStreams == null)
            {
                throw new ArgumentNullException(nameof(persistStreams));
            }
            return persistStreams.GetFromTo(Bucket.Default, start, end);
        }

        /// <summary>
        /// Deletes a stream from the default bucket.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="streamId">The stream id to be deleted.</param>
        public static void DeleteStream(this IPersistStreams persistStreams, string streamId)
        {
            if (persistStreams == null)
            {
                throw new ArgumentNullException(nameof(persistStreams));
            }
            persistStreams.DeleteStream(Bucket.Default, streamId);
        }

        /// <summary>
        /// Returns a single commit from any bucket.
        /// Wrapper for the <see cref="IPersistStreams.GetFromTo(long, long)"/> function in order to
        /// return a single commit.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        /// <param name="checkpointToken">The checkpoint token that mark the commit to read.</param>
        /// <returns>A single commit.</returns>
        public static ICommit GetCommit(this IPersistStreams persistStreams, Int64 checkpointToken)
        {
            return persistStreams.GetFromTo(checkpointToken - 1, checkpointToken).SingleOrDefault();
        }
    }
}