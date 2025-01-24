#pragma warning disable RCS1170 // Use read-only auto-implemented property.

namespace NEventStore
{
    /// <summary>
    /// Commit Attempt represents a single attempt to commit a set of events to a stream.
    /// </summary>
    public class CommitAttempt
    {
        /// <summary>
        ///     Initializes a new instance of the Commit class for the default bucket.
        /// </summary>
        /// <param name="streamId">The value which uniquely identifies the stream in a bucket to which the commit belongs.</param>
        /// <param name="streamRevision">The value which indicates the revision of the most recent event in the stream to which this commit applies.</param>
        /// <param name="commitId">The value which uniquely identifies the commit within the stream.</param>
        /// <param name="commitSequence">The value which indicates the sequence (or position) in the stream to which this commit applies.</param>
        /// <param name="commitStamp">The point in time at which the commit was persisted.</param>
        /// <param name="headers">The metadata which provides additional, unstructured information about this commit.</param>
        /// <param name="events">The collection of event messages to be committed as a single unit.</param>
        public CommitAttempt(
            Guid streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            IDictionary<string, object>? headers,
            ICollection<EventMessage> events)
            : this(Bucket.Default, streamId.ToString(), streamRevision, commitId, commitSequence, commitStamp, headers, events)
        { }

        /// <summary>
        ///     Initializes a new instance of the Commit class for the default bucket.
        /// </summary>
        /// <param name="streamId">The value which uniquely identifies the stream in a bucket to which the commit belongs.</param>
        /// <param name="streamRevision">The value which indicates the revision of the most recent event in the stream to which this commit applies.</param>
        /// <param name="commitId">The value which uniquely identifies the commit within the stream.</param>
        /// <param name="commitSequence">The value which indicates the sequence (or position) in the stream to which this commit applies.</param>
        /// <param name="commitStamp">The point in time at which the commit was persisted.</param>
        /// <param name="headers">The metadata which provides additional, unstructured information about this commit.</param>
        /// <param name="events">The collection of event messages to be committed as a single unit.</param>
        public CommitAttempt(
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            IDictionary<string, object>? headers,
            ICollection<EventMessage> events)
            : this(Bucket.Default, streamId, streamRevision, commitId, commitSequence, commitStamp, headers, events)
        { }

        /// <summary>
        ///     Initializes a new instance of the Commit class.
        /// </summary>
        /// <param name="bucketId">The value which identifies bucket to which the stream and the commit belongs</param>
        /// <param name="streamId">The value which uniquely identifies the stream in a bucket to which the commit belongs.</param>
        /// <param name="streamRevision">The value which indicates the revision of the most recent event in the stream to which this commit applies.</param>
        /// <param name="commitId">The value which uniquely identifies the commit within the stream.</param>
        /// <param name="commitSequence">The value which indicates the sequence (or position) in the stream to which this commit applies.</param>
        /// <param name="commitStamp">The point in time at which the commit was persisted.</param>
        /// <param name="headers">The metadata which provides additional, unstructured information about this commit.</param>
        /// <param name="events">The collection of event messages to be committed as a single unit.</param>
        public CommitAttempt(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            IDictionary<string, object>? headers,
            ICollection<EventMessage> events)
        {
            Guard.NotNullOrWhiteSpace(() => bucketId, bucketId);
            Guard.NotNullOrWhiteSpace(() => streamId, streamId);
            Guard.NotLessThanOrEqualTo(() => streamRevision, streamRevision, 0);
            Guard.NotDefault(() => commitId, commitId);
            Guard.NotLessThanOrEqualTo(() => commitSequence, commitSequence, 0);
            Guard.NotLessThan(() => commitSequence, streamRevision, 0);
            Guard.NotEmpty(() => events, events);

            BucketId = bucketId;
            StreamId = streamId;
            StreamRevision = streamRevision;
            CommitId = commitId;
            CommitSequence = commitSequence;
            CommitStamp = commitStamp;
            Headers = headers ?? new Dictionary<string, object>();
            Events = events;
            //Events = events == null ?
            //    new ReadOnlyCollection<EventMessage>(new List<EventMessage>()) :
            //    new ReadOnlyCollection<EventMessage>(events.ToList());
        }

        /// <summary>
        ///     Gets the value which identifies bucket to which the stream and the commit belongs.
        /// </summary>
        public string BucketId { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the commit belongs.
        /// </summary>
        public string StreamId { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
        /// </summary>
        public int StreamRevision { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the commit within the stream.
        /// </summary>
        public Guid CommitId { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
        /// </summary>
        public int CommitSequence { get; private set; }

        /// <summary>
        ///     Gets the point in time at which the commit was persisted.
        /// </summary>
        public DateTime CommitStamp { get; private set; }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this commit.
        /// </summary>
        public IDictionary<string, object> Headers { get; private set; }

        /// <summary>
        ///     Gets the collection of event messages to be committed as a single unit.
        /// </summary>
        public ICollection<EventMessage> Events { get; private set; }
    }
}

#pragma warning restore RCS1170 // Use read-only auto-implemented property.