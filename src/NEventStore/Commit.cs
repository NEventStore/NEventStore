namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents a series of events which have been fully committed as a single unit and which apply to the stream indicated.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Commit : ICommit
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
        public Commit(
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
        public Commit(
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
        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            IDictionary<string, object> headers,
            IEnumerable<IEventMessage> events)
            : this()
        {
            BucketId = bucketId;
            StreamId = streamId;
            CommitId = commitId;
            StreamRevision = streamRevision;
            CommitSequence = commitSequence;
            CommitStamp = commitStamp;
            Headers = headers ?? new Dictionary<string, object>();
            Events = events == null ? new List<IEventMessage>() : new List<IEventMessage>(events);
        }

        /// <summary>
        ///     Initializes a new instance of the Commit class.
        /// </summary>
        protected Commit()
        {}

        /// <summary>
        ///     Gets the value which identifies bucket to which the the stream and the the commit belongs.
        /// </summary>
        [DataMember]
        public virtual string BucketId { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the commit belongs.
        /// </summary>
        [DataMember]
        public virtual string StreamId { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
        /// </summary>
        [DataMember]
        public virtual int StreamRevision { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the commit within the stream.
        /// </summary>
        [DataMember]
        public virtual Guid CommitId { get; private set; }

        /// <summary>
        ///     Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
        /// </summary>
        [DataMember]
        public virtual int CommitSequence { get; private set; }

        /// <summary>
        ///     Gets the point in time at which the commit was persisted.
        /// </summary>
        [DataMember]
        public virtual DateTime CommitStamp { get; private set; }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this commit.
        /// </summary>
        [DataMember]
        public virtual IDictionary<string, object> Headers { get; private set; }

        /// <summary>
        ///     Gets the collection of event messages to be committed as a single unit.
        /// </summary>
        [DataMember]
        public virtual ICollection<IEventMessage> Events { get; private set; }

        [DataMember]
        public int Checkpoint { get; set; }

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>If the two objects are equal, returns true; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            var commit = obj as Commit;
            return commit != null && commit.StreamId == StreamId && commit.CommitId == CommitId;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return StreamId.GetHashCode() ^ CommitId.GetHashCode();
        }
    }
}