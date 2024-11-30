namespace NEventStore
{
    /// <summary>
    ///     Represents a materialized view of a stream at specific revision.
    /// </summary>
    public interface ISnapshot
    {
        /// <summary>
        /// Gets the value which uniquely identifies the bucket to which the snapshot applies.
        /// </summary>
        string BucketId { get; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the snapshot applies.
        /// </summary>
        string StreamId { get; }

        /// <summary>
        ///     Gets the position at which the snapshot applies.
        /// </summary>
        int StreamRevision { get; }

        /// <summary>
        ///     Gets the snapshot or materialized view of the stream at the revision indicated.
        /// </summary>
        object Payload { get; }
    }
}