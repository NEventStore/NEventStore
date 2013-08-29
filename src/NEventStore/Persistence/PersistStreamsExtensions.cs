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
        public static IEnumerable<Commit> GetFrom(this IPersistStreams persistStreams, DateTime start)
        {
            if (persistStreams == null)
            {
                throw new ArgumentException("persistStreams is null");
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
        public static IEnumerable<Commit> GetFromTo(this IPersistStreams persistStreams, DateTime start, DateTime end)
        {
            if (persistStreams == null)
            {
                throw new ArgumentException("persistStreams is null");
            }
            return persistStreams.GetFromTo(Bucket.Default, start, end);
        }

        /// <summary>
        /// Gets all commits after from the specified checkpoint with default batchSize of 100.
        /// </summary>
        /// <param name="checkpoint">The checkpoint.</param>
        /// <returns></returns>
        public static IEnumerable<Commit> GetSince(this IPersistStreams persistStreams, int checkpoint)
        {
            if (persistStreams == null)
            {
                throw new ArgumentException("persistStreams is null");
            }
            return persistStreams.GetSince(checkpoint, 100);
        }
    }
}