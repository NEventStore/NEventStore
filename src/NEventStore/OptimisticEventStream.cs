namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using NEventStore.Logging;

    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This behaves like a stream--not a .NET 'Stream' object, but a stream nonetheless.")]
    public sealed class OptimisticEventStream : IEventStream
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(OptimisticEventStream));
        private readonly ICollection<EventMessage> _committed = new LinkedList<EventMessage>();
        private readonly ImmutableCollection<EventMessage> _committedImmutableWrapper;
        private readonly IDictionary<string, object> _committedHeaders = new Dictionary<string, object>();
        private readonly ImmutableDictionary<string, object> _committedHeadersImmutableWrapper;
        private readonly ICollection<EventMessage> _events = new LinkedList<EventMessage>();
        private readonly ImmutableCollection<EventMessage> _eventsImmutableWraper;
        private readonly ICollection<Guid> _identifiers = new HashSet<Guid>();
        private readonly ICommitEvents _persistence;
        private bool _disposed;
        // a stream is considered partial if we haven't read all the events in a commit
        private bool _isPartialStream;

        public string BucketId { get; }
        public string StreamId { get; }
        public int StreamRevision { get; private set; }
        public int CommitSequence { get; private set; }
        public ICollection<EventMessage> CommittedEvents { get => _committedImmutableWrapper; }
        public IDictionary<string, object> CommittedHeaders { get => _committedHeadersImmutableWrapper; }
        public ICollection<EventMessage> UncommittedEvents { get => _eventsImmutableWraper; }
        public IDictionary<string, object> UncommittedHeaders { get; } = new Dictionary<string, object>();

        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence)
        {
            BucketId = bucketId;
            StreamId = streamId;
            _persistence = persistence;
            _committedImmutableWrapper = new ImmutableCollection<EventMessage>(_committed);
            _eventsImmutableWraper = new ImmutableCollection<EventMessage>(_events);
            _committedHeadersImmutableWrapper = new ImmutableDictionary<string, object>(_committedHeaders);
        }

        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence, int minRevision, int maxRevision)
            : this(bucketId, streamId, persistence)
        {
            IEnumerable<ICommit> commits = persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
            PopulateStream(minRevision, maxRevision, commits);

            if (minRevision > 0 && _committed.Count == 0)
            {
                throw new StreamNotFoundException(String.Format(Messages.StreamNotFoundException, streamId, BucketId));
            }
        }

        public OptimisticEventStream(ISnapshot snapshot, ICommitEvents persistence, int maxRevision)
            : this(snapshot.BucketId, snapshot.StreamId, persistence)
        {
            IEnumerable<ICommit> commits = persistence.GetFrom(snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision, maxRevision);
            PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
            StreamRevision = snapshot.StreamRevision + _committed.Count;
        }

        public void Add(EventMessage uncommittedEvent)
        {
            if (uncommittedEvent == null)
            {
                throw new ArgumentNullException(nameof(uncommittedEvent));
            }

            if (uncommittedEvent.Body == null)
            {
                throw new ArgumentNullException(nameof(uncommittedEvent.Body));
            }

            if (Logger.IsVerboseEnabled) Logger.Verbose(Resources.AppendingUncommittedToStream, uncommittedEvent.Body.GetType(), StreamId);
            _events.Add(uncommittedEvent);
        }

        public void CommitChanges(Guid commitId)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Resources.AttemptingToCommitChanges, StreamId);

            if (_isPartialStream)
            {
                throw new ConcurrencyException();
            }

            if (_identifiers.Contains(commitId))
            {
                throw new DuplicateCommitException(String.Format(Messages.DuplicateCommitIdException, commitId));
            }

            if (!HasChanges())
            {
                return;
            }

            try
            {
                PersistChanges(commitId);
            }
            catch (ConcurrencyException cex)
            {
                if (Logger.IsDebugEnabled) Logger.Debug(Resources.UnderlyingStreamHasChanged, StreamId, cex.Message); //not useful to log info because the exception will be thrown 
                IEnumerable<ICommit> commits = _persistence.GetFrom(BucketId, StreamId, StreamRevision + 1, int.MaxValue);
                PopulateStream(StreamRevision + 1, int.MaxValue, commits);

                throw;
            }
        }

        public void ClearChanges()
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Resources.ClearingUncommittedChanges, StreamId);
            _events.Clear();
            UncommittedHeaders.Clear();
        }

        private void PopulateStream(int minRevision, int maxRevision, IEnumerable<ICommit> commits)
        {
            _isPartialStream = false;
            foreach (var commit in commits ?? Enumerable.Empty<ICommit>())
            {
                _identifiers.Add(commit.CommitId);

                int currentRevision = commit.StreamRevision - commit.Events.Count + 1;
                // just in case the persistence returned more commits than it should be
                if (currentRevision > maxRevision)
                {
                    _isPartialStream = true;
                    if (Logger.IsDebugEnabled) Logger.Debug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    return;
                }

                if (Logger.IsVerboseEnabled) Logger.Verbose(Resources.AddingCommitsToStream, commit.CommitId, commit.Events.Count, StreamId);

                CommitSequence = commit.CommitSequence;

                CopyToCommittedHeaders(commit);
                CopyToEvents(minRevision, maxRevision, currentRevision, commit);
            }
        }

        private void CopyToCommittedHeaders(ICommit commit)
        {
            foreach (var key in commit.Headers.Keys)
            {
                _committedHeaders[key] = commit.Headers[key];
            }
        }

        private void CopyToEvents(int minRevision, int maxRevision, int currentRevision, ICommit commit)
        {
            foreach (var @event in commit.Events)
            {
                if (currentRevision > maxRevision)
                {
                    _isPartialStream = true;
                    if (Logger.IsDebugEnabled) Logger.Debug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    break;
                }

                if (currentRevision++ < minRevision)
                {
                    if (Logger.IsDebugEnabled) Logger.Debug(Resources.IgnoringBeforeRevision, commit.CommitId, StreamId, maxRevision);
                    continue;
                }

                _committed.Add(@event);
                StreamRevision = currentRevision - 1;
            }
        }

        private bool HasChanges()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Resources.AlreadyDisposed);
            }

            if (_events.Count > 0)
            {
                return true;
            }

            if (Logger.IsInfoEnabled) Logger.Info(Resources.NoChangesToCommit, StreamId);
            return false;
        }

        private void PersistChanges(Guid commitId)
        {
            CommitAttempt attempt = BuildCommitAttempt(commitId);

            if (Logger.IsDebugEnabled) Logger.Debug(Resources.PersistingCommit, commitId, StreamId, attempt.Events?.Count ?? 0);
            ICommit commit = _persistence.Commit(attempt);

            PopulateStream(StreamRevision + 1, attempt.StreamRevision, new[] { commit });
            ClearChanges();
        }

        private CommitAttempt BuildCommitAttempt(Guid commitId)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Resources.BuildingCommitAttempt, commitId, StreamId);
            return new CommitAttempt(
                BucketId,
                StreamId,
                StreamRevision + _events.Count,
                commitId,
                CommitSequence + 1,
                SystemTime.UtcNow,
                UncommittedHeaders.ToDictionary(x => x.Key, x => x.Value),
                _events.ToList());
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}