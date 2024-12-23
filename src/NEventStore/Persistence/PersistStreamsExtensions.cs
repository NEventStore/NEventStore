namespace NEventStore.Persistence
{
    /// <summary>
    ///    Provides a set of extension methods for the <see cref="IPersistStreams"/> interface.
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