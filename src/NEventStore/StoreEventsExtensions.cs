namespace NEventStore
{
    using System;
    using NEventStore.Persistence;

    public static class StoreEventsExtensions
    {
        /// <summary>
        ///     Creates a new stream.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="streamId">The value which uniquely identifies the stream to be created.</param>
        /// <returns>An empty stream.</returns>
        public static IEventStream CreateStream(this IStoreEvents storeEvents, Guid streamId)
        {
            return CreateStream(storeEvents, Bucket.Default, streamId);
        }

        /// <summary>
        ///     Creates a new stream.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="streamId">The value which uniquely identifies the stream to be created.</param>
        /// <returns>An empty stream.</returns>
        public static IEventStream CreateStream(this IStoreEvents storeEvents, string streamId)
        {
            EnsureStoreEventsNotNull(storeEvents);
            return storeEvents.CreateStream(Bucket.Default, streamId);
        }

        /// <summary>
        ///     Creates a new stream.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The value which uniquely identifies the stream within the bucket to be created.</param>
        /// <returns>An empty stream.</returns>
        public static IEventStream CreateStream(this IStoreEvents storeEvents, string bucketId, Guid streamId)
        {
            EnsureStoreEventsNotNull(storeEvents);
            return storeEvents.CreateStream(bucketId, streamId.ToString());
        }

        /// <summary>
        ///     Reads the stream indicated from the minimum revision specified up to the maximum revision specified or creates
        ///     an empty stream if no commits are found and a minimum revision of zero is provided.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="streamId">The value which uniquely identifies the stream from which the events will be read.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <returns>A series of committed events represented as a stream.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        /// <exception cref="StreamNotFoundException" />
        public static IEventStream OpenStream(this IStoreEvents storeEvents, Guid streamId, int minRevision = int.MinValue, int maxRevision = int.MaxValue)
        {
            return OpenStream(storeEvents, Bucket.Default, streamId, minRevision, maxRevision);
        }

        /// <summary>
        ///     Reads the stream indicated from the minimum revision specified up to the maximum revision specified or creates
        ///     an empty stream if no commits are found and a minimum revision of zero is provided.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="streamId">The value which uniquely identifies the stream from which the events will be read.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <returns>A series of committed events represented as a stream.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        /// <exception cref="StreamNotFoundException" />
        public static IEventStream OpenStream(this IStoreEvents storeEvents, string streamId, int minRevision = int.MinValue, int maxRevision = int.MaxValue)
        {
            EnsureStoreEventsNotNull(storeEvents);
            return storeEvents.OpenStream(Bucket.Default, streamId, minRevision, maxRevision);
        }

        /// <summary>
        ///     Reads the stream indicated from the minimum revision specified up to the maximum revision specified or creates
        ///     an empty stream if no commits are found and a minimum revision of zero is provided.
        /// </summary>
        /// <param name="storeEvents">The store events instance.</param>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The value which uniquely identifies the stream within the bucket to be created.</param>
        /// <param name="minRevision">The minimum revision of the stream to be read.</param>
        /// <param name="maxRevision">The maximum revision of the stream to be read.</param>
        /// <returns>A series of committed events represented as a stream.</returns>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        /// <exception cref="StreamNotFoundException" />
        public static IEventStream OpenStream(this IStoreEvents storeEvents, string bucketId, Guid streamId, int minRevision = int.MinValue, int maxRevision = int.MaxValue)
        {
            EnsureStoreEventsNotNull(storeEvents);
            return storeEvents.OpenStream(bucketId, streamId.ToString(), minRevision, maxRevision);
        }

        private static void EnsureStoreEventsNotNull(IStoreEvents storeEvents)
        {
            if (storeEvents == null)
            {
                throw new ArgumentException("storeEvents is null");
            }
        }
    }
}