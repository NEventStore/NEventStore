#pragma warning disable RCS1170 // Use read-only auto-implemented property.

namespace NEventStore.Persistence
{
    /// <summary>
    ///     Indicates the most recent information representing the head of a given stream.
    /// </summary>
    public class StreamHead : IStreamHead
    {
        /// <summary>
        ///     Initializes a new instance of the StreamHead class.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The value which uniquely identifies the stream in the bucket where the last snapshot exceeds the allowed threshold.</param>
        /// <param name="headRevision">The value which indicates the revision, length, or number of events committed to the stream.</param>
        /// <param name="snapshotRevision">The value which indicates the revision at which the last snapshot was taken.</param>
        public StreamHead(string bucketId, string streamId, int headRevision, int snapshotRevision)
        {
            BucketId = bucketId;
            StreamId = streamId;
            HeadRevision = headRevision;
            SnapshotRevision = snapshotRevision;
        }

        /// <summary>
        /// Stream Head Equality Comparer
        /// </summary>
        public static IEqualityComparer<StreamHead> StreamIdBucketIdComparer { get; } = new StreamHeadEqualityComparer();

        /// <summary>
        ///     Gets the value which uniquely identifies the stream where the last snapshot exceeds the allowed threshold.
        /// </summary>
        public string BucketId { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream where the last snapshot exceeds the allowed threshold.
        /// </summary>
        public string StreamId { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the revision, length, or number of events committed to the stream.
        /// </summary>
        public int HeadRevision { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the revision at which the last snapshot was taken.
        /// </summary>
        public int SnapshotRevision { get; private set; }

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>If the two objects are equal, returns true; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return obj is StreamHead commit
                && commit.StreamId == StreamId;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return StreamId.GetHashCode();
        }
    }
}

#pragma warning restore RCS1170 // Use read-only auto-implemented property.