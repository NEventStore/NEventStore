namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;

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
        ///     Gets all commits after from start checkpoint.
        /// </summary>
        /// <param name="persistStreams">The IPersistStreams instance.</param>
        public static IEnumerable<ICommit> GetFromStart(this IPersistStreams persistStreams)
        {
            if (persistStreams == null)
            {
                throw new ArgumentNullException(nameof(persistStreams));
            }
            return persistStreams.GetFrom(0);
        }
    }
}