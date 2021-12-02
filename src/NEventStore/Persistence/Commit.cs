#pragma warning disable RCS1170 // Use read-only auto-implemented property.

namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;

    public class Commit : ICommit
    {
        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            Int64 checkpointToken,
            IDictionary<string, object> headers,
            ICollection<EventMessage> events)
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

        public string BucketId { get; private set; }

        public string StreamId { get; private set; }

        public int StreamRevision { get; private set; }

        public Guid CommitId { get; private set; }

        public int CommitSequence { get; private set; }

        public DateTime CommitStamp { get; private set; }

        public IDictionary<string, object> Headers { get; private set; }

        public ICollection<EventMessage> Events { get; private set; }

        public Int64 CheckpointToken { get; private set; }
    }
}

#pragma warning restore RCS1170 // Use read-only auto-implemented property.