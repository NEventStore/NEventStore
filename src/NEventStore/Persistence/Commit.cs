using System;
using System.Collections.Generic;

#pragma warning disable RCS1170 // Use read-only auto-implemented property.

namespace NEventStore.Persistence
{
    public class Commit : ICommit
    {
        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            long checkpointToken,
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

        public string BucketId { get; }

        public string StreamId { get; }

        public int StreamRevision { get; }

        public Guid CommitId { get; }

        public int CommitSequence { get; }

        public DateTime CommitStamp { get; }

        public IDictionary<string, object> Headers { get; }

        public ICollection<EventMessage> Events { get; }

        public long CheckpointToken { get; }
    }
}

#pragma warning restore RCS1170 // Use read-only auto-implemented property.