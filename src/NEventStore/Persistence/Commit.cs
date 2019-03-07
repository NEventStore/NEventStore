namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Commit : ICommit
    {
        private readonly string _bucketId;
        private readonly string _streamId;
        private readonly int _streamRevision;
        private readonly Guid _commitId;
        private readonly int _commitSequence;
        private readonly DateTime _commitStamp;
        private readonly IDictionary<string, object> _headers;
        private readonly ICollection<EventMessage> _events;
        private readonly Int64 _checkpointToken;

        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            Int64 checkpointToken,
            IDictionary<string, object> headers,
            IEnumerable<EventMessage> events)
        {
            _bucketId = bucketId;
            _streamId = streamId;
            _streamRevision = streamRevision;
            _commitId = commitId;
            _commitSequence = commitSequence;
            _commitStamp = commitStamp;
            _checkpointToken = checkpointToken;
            _headers = headers ?? new Dictionary<string, object>();
            _events = events == null ?
                new ReadOnlyCollection<EventMessage>(new List<EventMessage>()) :
                new ReadOnlyCollection<EventMessage>(new List<EventMessage>(events));
        }

        public string BucketId
        {
            get { return _bucketId; }
        }

        public string StreamId
        {
            get { return _streamId; }
        }

        public int StreamRevision
        {
            get { return _streamRevision; }
        }

        public Guid CommitId
        {
            get { return _commitId; }
        }

        public int CommitSequence
        {
            get { return _commitSequence; }
        }

        public DateTime CommitStamp
        {
            get { return _commitStamp; }
        }

        public IDictionary<string, object> Headers
        {
            get { return _headers; }
        }

        public ICollection<EventMessage> Events
        {
            get
            {
                return _events;
            }
        }

        public Int64 CheckpointToken
        {
            get
            {
                return _checkpointToken;
            }
        }
    }
}