namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CommitAttempt
    {
        private readonly string _bucketId;
        private readonly string _streamId;
        private readonly int _streamRevision;
        private readonly Guid _commitId;
        private readonly int _commitSequence;
        private readonly DateTime _commitStamp;
        private readonly IDictionary<string, object> _headers;
        private readonly ICollection<IEventMessage> _events;

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
            IDictionary<string, object> headers,
            IEnumerable<IEventMessage> events)
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
            IDictionary<string, object> headers,
            IEnumerable<IEventMessage> events)
            : this(Bucket.Default, streamId, streamRevision, commitId, commitSequence, commitStamp, headers, events)
        {}

        /// <summary>
        ///     Initializes a new instance of the Commit class.
        /// </summary>
        /// <param name="bucketId">The value which identifies bucket to which the the stream and the the commit belongs</param>
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
            IDictionary<string, object> headers,
            IEnumerable<IEventMessage> events)
        {
            //TODO write tests for these?
            Guard.NotNullOrWhiteSpace(() => bucketId, bucketId);
            Guard.NotNullOrWhiteSpace(() => streamId, streamId);
            Guard.NotLessThanOrEqualTo(() => streamRevision, streamRevision, 0);
            Guard.NotDefault(() => commitId, commitId);
            Guard.NotLessThanOrEqualTo(() => commitSequence, commitSequence, 0);
            Guard.NotEmpty(() => events, events);

            _bucketId = bucketId;
            _streamId = streamId;
            _streamRevision = streamRevision;
            _commitId = commitId;
            _commitSequence = commitSequence;
            _commitStamp = commitStamp;
            _headers = headers ?? new Dictionary<string, object>();
            _events = events == null ?
                new ReadOnlyCollection<IEventMessage>(new List<IEventMessage>()) :
                new ReadOnlyCollection<IEventMessage>(new List<IEventMessage>(events));
        }

        /// <summary>
        ///     Gets the value which identifies bucket to which the the stream and the the commit belongs.
        /// </summary>
        public string BucketId
        {
            get { return _bucketId; }
        }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the commit belongs.
        /// </summary>
        public string StreamId
        {
            get { return _streamId; }
        }

        /// <summary>
        ///     Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
        /// </summary>
        public int StreamRevision
        {
            get { return _streamRevision; }
        }

        /// <summary>
        ///     Gets the value which uniquely identifies the commit within the stream.
        /// </summary>
        public Guid CommitId
        {
            get { return _commitId; }
        }

        /// <summary>
        ///     Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
        /// </summary>
        public int CommitSequence
        {
            get { return _commitSequence; }
        }

        /// <summary>
        ///     Gets the point in time at which the commit was persisted.
        /// </summary>
        public DateTime CommitStamp
        {
            get { return _commitStamp; }
        }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this commit.
        /// </summary>
        public IDictionary<string, object> Headers 
        {
            get { return _headers; }
        }

        /// <summary>
        ///     Gets the collection of event messages to be committed as a single unit.
        /// </summary>
        public ICollection<IEventMessage> Events
        {
            get
            {
                return _events;
            }
        }
    }
}