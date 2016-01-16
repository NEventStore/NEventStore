namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    /// <summary>
    ///     Tracks the heads of streams to reduce latency by avoiding roundtrips to storage.
    /// </summary>
    public class OptimisticPipelineHook : PipelineHookBase
    {
        private const int MaxStreamsToTrack = 100;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (OptimisticPipelineHook));
        private readonly Dictionary<HeadKey, ICommit> _heads = new Dictionary<HeadKey, ICommit>(); //TODO use concurrent collections
        private readonly LinkedList<HeadKey> _maxItemsToTrack = new LinkedList<HeadKey>();
        private readonly int _maxStreamsToTrack;

        public OptimisticPipelineHook()
            : this(MaxStreamsToTrack)
        {}

        public OptimisticPipelineHook(int maxStreamsToTrack)
        {
            Logger.Debug(Resources.TrackingStreams, maxStreamsToTrack);
            _maxStreamsToTrack = maxStreamsToTrack;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override ICommit Select(ICommit committed)
        {
            Track(committed);
            return committed;
        }

        public override bool PreCommit(CommitAttempt attempt)
        {
            Logger.Debug(Resources.OptimisticConcurrencyCheck, attempt.StreamId);

            ICommit head = GetStreamHead(GetHeadKey(attempt));
            if (head == null)
            {
                return true;
            }

            if (head.CommitSequence >= attempt.CommitSequence)
            {
                throw new ConcurrencyException();
            }

            if (head.StreamRevision >= attempt.StreamRevision - attempt.Events.Count)
            {
                throw new ConcurrencyException();
            }

            if (head.CommitSequence < attempt.CommitSequence - 1)
            {
                throw new StorageException(); // beyond the end of the stream
            }

            if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
            {
                throw new StorageException(); // beyond the end of the stream
            }

            Logger.Debug(Resources.NoConflicts, attempt.StreamId);
            return true;
        }

        public override void PostCommit(ICommit committed)
        {
            Track(committed);
        }

        public override void OnPurge(string bucketId)
        {
            lock (_maxItemsToTrack)
            {
                if (bucketId == null)
                {
                    _heads.Clear();
                    _maxItemsToTrack.Clear();
                    return;
                }
                HeadKey[] headsInBucket = _heads.Keys.Where(k => k.BucketId == bucketId).ToArray();
                foreach (var head in headsInBucket)
                {
                    RemoveHead(head);
                }
            }
        }

        public override void OnDeleteStream(string bucketId, string streamId)
        {
            lock (_maxItemsToTrack)
            {
                RemoveHead(new HeadKey(bucketId, streamId));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _heads.Clear();
            _maxItemsToTrack.Clear();
        }

        public virtual void Track(ICommit committed)
        {
            if (committed == null)
            {
                return;
            }

            lock (_maxItemsToTrack)
            {
                UpdateStreamHead(committed);
                TrackUpToCapacity(committed);
            }
        }

        private void UpdateStreamHead(ICommit committed)
        {
            HeadKey headKey = GetHeadKey(committed);
            ICommit head = GetStreamHead(headKey);
            if (AlreadyTracked(head))
            {
                _maxItemsToTrack.Remove(headKey);
            }

            head = head ?? committed;
            head = head.StreamRevision > committed.StreamRevision ? head : committed;

            _heads[headKey] = head;
        }

        private void RemoveHead(HeadKey head)
        {
            _heads.Remove(head);
            LinkedListNode<HeadKey> node = _maxItemsToTrack.Find(head); // There should only be ever one or none
            if (node != null)
            {
                _maxItemsToTrack.Remove(node);
            }
        }

        private static bool AlreadyTracked(ICommit head)
        {
            return head != null;
        }

        private void TrackUpToCapacity(ICommit committed)
        {
            Logger.Verbose(Resources.TrackingCommit, committed.CommitSequence, committed.StreamId);
            _maxItemsToTrack.AddFirst(GetHeadKey(committed));
            if (_maxItemsToTrack.Count <= _maxStreamsToTrack)
            {
                return;
            }

            HeadKey expired = _maxItemsToTrack.Last.Value;
            Logger.Verbose(Resources.NoLongerTrackingStream, expired);

            _heads.Remove(expired);
            _maxItemsToTrack.RemoveLast();
        }

        public virtual bool Contains(ICommit attempt)
        {
            return GetStreamHead(GetHeadKey(attempt)) != null;
        }

        private ICommit GetStreamHead(HeadKey headKey)
        {
            lock (_maxItemsToTrack)
            {
                ICommit head;
                _heads.TryGetValue(headKey, out head);
                return head;
            }
        }

        private static HeadKey GetHeadKey(ICommit commit)
        {
            return new HeadKey(commit.BucketId, commit.StreamId);
        }

        private static HeadKey GetHeadKey(CommitAttempt commitAttempt)
        {
            return new HeadKey(commitAttempt.BucketId, commitAttempt.StreamId);
        }

        private sealed class HeadKey : IEquatable<HeadKey>
        {
            private readonly string _bucketId;

            private readonly string _streamId;

            public HeadKey(string bucketId, string streamId)
            {
                _bucketId = bucketId;
                _streamId = streamId;
            }

            public string BucketId
            {
                get { return _bucketId; }
            }

            public string StreamId
            {
                get { return _streamId; }
            }

            public bool Equals(HeadKey other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }
                return String.Equals(_bucketId, other._bucketId) && String.Equals(_streamId, other._streamId);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                return obj is HeadKey && Equals((HeadKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_bucketId.GetHashCode()*397) ^ _streamId.GetHashCode();
                }
            }
        }
    }
}