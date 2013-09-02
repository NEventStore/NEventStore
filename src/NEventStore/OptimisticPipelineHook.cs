namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    /// <summary>
    ///     Tracks the heads of streams to reduce latency by avoiding roundtrips to storage.
    /// </summary>
    public class OptimisticPipelineHook : IPipelineHook
    {
        private const int MaxStreamsToTrack = 100;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (OptimisticPipelineHook));
        private readonly IDictionary<string, ICommit> _heads = new Dictionary<string, ICommit>();
        private readonly LinkedList<string> _maxItemsToTrack = new LinkedList<string>();
        private readonly int _maxStreamsToTrack;

        public OptimisticPipelineHook()
            : this(MaxStreamsToTrack)
        {}

        public OptimisticPipelineHook(int maxStreamsToTrack)
        {
            Logger.Debug(Resources.TrackingStreams, maxStreamsToTrack);
            _maxStreamsToTrack = maxStreamsToTrack;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual ICommit Select(ICommit committed)
        {
            Track(committed);
            return committed;
        }

        public virtual bool PreCommit(ICommit attempt)
        {
            Logger.Debug(Resources.OptimisticConcurrencyCheck, attempt.StreamId);

            ICommit head = GetStreamHead(attempt.StreamId);
            if (head == null)
            {
                return true;
            }

            if (head.CommitSequence >= attempt.CommitSequence)
            {
                throw new ConcurrencyException();
            }

            if (head.StreamRevision >= attempt.StreamRevision)
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

        public virtual void PostCommit(ICommit committed)
        {
            Track(committed);
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
            ICommit head = GetStreamHead(committed.StreamId);
            if (AlreadyTracked(head))
            {
                _maxItemsToTrack.Remove(committed.StreamId);
            }

            head = head ?? committed;
            head = head.StreamRevision > committed.StreamRevision ? head : committed;

            _heads[committed.StreamId] = head;
        }

        private static bool AlreadyTracked(ICommit head)
        {
            return head != null;
        }

        private void TrackUpToCapacity(ICommit committed)
        {
            Logger.Verbose(Resources.TrackingCommit, committed.CommitSequence, committed.StreamId);
            _maxItemsToTrack.AddFirst(committed.StreamId);
            if (_maxItemsToTrack.Count <= _maxStreamsToTrack)
            {
                return;
            }

            string expired = _maxItemsToTrack.Last.Value;
            Logger.Verbose(Resources.NoLongerTrackingStream, expired);

            _heads.Remove(expired);
            _maxItemsToTrack.RemoveLast();
        }

        public virtual bool Contains(ICommit attempt)
        {
            return GetStreamHead(attempt.StreamId) != null;
        }

        private ICommit GetStreamHead(string streamId)
        {
            lock (_maxItemsToTrack)
            {
                ICommit head;
                _heads.TryGetValue(streamId, out head);
                return head;
            }
        }
    }
}