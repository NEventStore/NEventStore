namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Commit : ICommit
    {
        private static readonly ReadOnlyCollection<EventMessage> EmptyEventMessageCollection = new ReadOnlyCollection<EventMessage>(new List<EventMessage>());

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
                EmptyEventMessageCollection :
                new ReadOnlyCollection<EventMessage>(new List<EventMessage>(events));
        }

        /// <summary>
        /// Overloaded constructor: internally we wrap the connection in a ReadOnlyCollection
        /// which accepts an IList. Creating a new List from a collection of elements 
        /// might be an expensive operations, let's avoid useless operations if we already have a list.
        /// </summary>
        /// <param name="bucketId"></param>
        /// <param name="streamId"></param>
        /// <param name="streamRevision"></param>
        /// <param name="commitId"></param>
        /// <param name="commitSequence"></param>
        /// <param name="commitStamp"></param>
        /// <param name="checkpointToken"></param>
        /// <param name="headers"></param>
        /// <param name="events"></param>
        public Commit(
            string bucketId,
            string streamId,
            int streamRevision,
            Guid commitId,
            int commitSequence,
            DateTime commitStamp,
            Int64 checkpointToken,
            IDictionary<string, object> headers,
            IList<EventMessage> events)
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
                EmptyEventMessageCollection :
                new ReadOnlyCollection<EventMessage>(events);
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