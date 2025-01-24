#pragma warning disable RCS1170 // Use read-only auto-implemented property.

namespace NEventStore.Persistence
{
    /// <summary>
    /// Represents a commit to the event store.
    /// </summary>
    public class Commit : ICommit
    {
        /// <summary>
        /// Initializes a new instance of the Commit class.
        /// </summary>
        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            Int64 checkpointToken,
            IDictionary<string, object>? headers,
            ICollection<EventMessage>? events)
        {
            BucketId = bucketId;
            StreamId = streamId;
            StreamRevision = streamRevision;
            CommitId = commitId;
            CommitSequence = commitSequence;
            CommitStamp = commitStamp;
            CheckpointToken = checkpointToken;
            Headers = headers ?? new Dictionary<string, object>();
            Events = events ?? Array.Empty<EventMessage>();
            //Events = events == null ?
            //    new ReadOnlyCollection<EventMessage>(new List<EventMessage>()) :
            //    new ReadOnlyCollection<EventMessage>(new List<EventMessage>(events));
        }

        /// <inheritdoc/>
        public string BucketId { get; private set; }

        /// <inheritdoc/>
        public string StreamId { get; private set; }

        /// <inheritdoc/>
        public int StreamRevision { get; private set; }

        /// <inheritdoc/>
        public Guid CommitId { get; private set; }

        /// <inheritdoc/>
        public int CommitSequence { get; private set; }

        /// <inheritdoc/>
        public DateTime CommitStamp { get; private set; }

        /// <inheritdoc/>
        public IDictionary<string, object> Headers { get; private set; }

        /// <inheritdoc/>
        public ICollection<EventMessage> Events { get; private set; }

        /// <inheritdoc/>
        public Int64 CheckpointToken { get; private set; }
    }
}

#pragma warning restore RCS1170 // Use read-only auto-implemented property.