namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    /// <summary>
    ///     Tracks the heads of streams to reduce latency by avoiding roundtrips to storage.
    /// </summary>
    public class OptimisticPipelineHook : PipelineHookBase
    {
        internal const int MaxStreamsToTrack = 100;
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticPipelineHook));
        private readonly Dictionary<HeadKey, ICommit> _heads = new Dictionary<HeadKey, ICommit>(); //TODO use concurrent collections
        private readonly LinkedList<HeadKey> _maxItemsToTrack = new LinkedList<HeadKey>();
        private readonly int _maxStreamsToTrack;

        public OptimisticPipelineHook()
            : this(MaxStreamsToTrack)
        { }

        public OptimisticPipelineHook(int maxStreamsToTrack)
        {
            Logger.LogDebug(Resources.TrackingStreams, maxStreamsToTrack);
            _maxStreamsToTrack = maxStreamsToTrack;
        }

        public override ICommit Select(ICommit committed)
        {
            Track(committed);
            return committed;
        }

        public override bool PreCommit(CommitAttempt attempt)
        {
            Logger.LogTrace(Resources.OptimisticConcurrencyCheck, attempt.StreamId);

            ICommit head = GetStreamHead(GetHeadKey(attempt));
            if (head == null)
            {
                return true;
            }

            if (head.CommitSequence >= attempt.CommitSequence)
            {
                throw new ConcurrencyException(String.Format(
                    Messages.ConcurrencyExceptionCommitSequence,
                    head.CommitSequence,
                    attempt.BucketId,
                    attempt.CommitSequence,
                    attempt.StreamId,
                    attempt.StreamRevision,
                    attempt.Events.Count
                ));
            }

            if (head.StreamRevision >= attempt.StreamRevision)
            {
                throw new ConcurrencyException(String.Format(
                    Messages.ConcurrencyExceptionStreamRevision,
                    head.StreamRevision,
                    attempt.BucketId,
                    attempt.StreamId,
                    attempt.StreamRevision,
                    attempt.Events.Count
                ));
            }

            if (head.CommitSequence < attempt.CommitSequence - 1)
            {
                throw new StorageException(String.Format(
                     Messages.StorageExceptionCommitSequence,
                     head.CommitSequence,
                     attempt.BucketId,
                     attempt.CommitSequence,
                     attempt.StreamId,
                     attempt.StreamRevision,
                     attempt.Events.Count
                 )); // beyond the end of the stream
            }

            if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
            {
                throw new StorageException(String.Format(
                     Messages.StorageExceptionEndOfStream,
                     head.StreamRevision,
                     attempt.StreamRevision,
                     attempt.Events.Count,
                     attempt.BucketId,
                     attempt.StreamId,
                     attempt.StreamRevision
                 )); // beyond the end of the stream
            }

            Logger.LogTrace(Resources.NoConflicts, attempt.StreamId, attempt.BucketId);
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

        protected override void Dispose(bool disposing)
        {
            _heads.Clear();
            _maxItemsToTrack.Clear();
            base.Dispose(disposing);
        }

        public void Track(ICommit committed)
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
            Logger.LogTrace(Resources.TrackingCommit, committed.CommitSequence, committed.StreamId, committed.BucketId);
            _maxItemsToTrack.AddFirst(GetHeadKey(committed));
            if (_maxItemsToTrack.Count <= _maxStreamsToTrack)
            {
                return;
            }

            HeadKey expired = _maxItemsToTrack.Last.Value;
            Logger.LogTrace(Resources.NoLongerTrackingStream, expired.StreamId, expired.BucketId);

            _heads.Remove(expired);
            _maxItemsToTrack.RemoveLast();
        }

        public bool Contains(ICommit attempt)
        {
            return GetStreamHead(GetHeadKey(attempt)) != null;
        }

        private ICommit GetStreamHead(HeadKey headKey)
        {
            lock (_maxItemsToTrack)
            {
                _heads.TryGetValue(headKey, out ICommit head);
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
            public string BucketId { get; }

            public string StreamId { get; }

            public HeadKey(string bucketId, string streamId)
            {
                BucketId = bucketId;
                StreamId = streamId;
            }

            public bool Equals(HeadKey other)
            {
                if (other is null)
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }
                return String.Equals(BucketId, other.BucketId)
                    && String.Equals(StreamId, other.StreamId);
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                return obj is HeadKey headKey && Equals(headKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (BucketId.GetHashCode() * 397) ^ StreamId.GetHashCode();
                }
            }
        }
    }
}