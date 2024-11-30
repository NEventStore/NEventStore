using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore
{
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This behaves like a stream--not a .NET 'Stream' object, but a stream nonetheless.")]
    public sealed class OptimisticEventStream : IEventStream
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticEventStream));
        private readonly ICollection<EventMessage> _committed = new LinkedList<EventMessage>();
        private readonly IDictionary<string, object> _committedHeaders = new Dictionary<string, object>();
        private readonly ImmutableDictionary<string, object> _committedHeadersImmutableWrapper;
        private readonly ImmutableCollection<EventMessage> _committedImmutableWrapper;
        private readonly ICollection<EventMessage> _events = new LinkedList<EventMessage>();
        private readonly ImmutableCollection<EventMessage> _eventsImmutableWraper;
        private readonly ICollection<Guid> _identifiers = new HashSet<Guid>();
        private readonly ICommitEvents _persistence;

        private bool _disposed;

        // a stream is considered partial if we haven't read all the events in a commit
        private bool _isPartialStream;

        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence)
        {
            BucketId = bucketId;
            StreamId = streamId;
            _persistence = persistence;
            _committedImmutableWrapper = new ImmutableCollection<EventMessage>(_committed);
            _eventsImmutableWraper = new ImmutableCollection<EventMessage>(_events);
            _committedHeadersImmutableWrapper = new ImmutableDictionary<string, object>(_committedHeaders);
        }

        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence, int minRevision,
            int maxRevision)
            : this(bucketId, streamId, persistence)
        {
            var commits = persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
            PopulateStream(minRevision, maxRevision, commits);

            if (minRevision > 0 && _committed.Count == 0)
                throw new StreamNotFoundException(string.Format(Messages.StreamNotFoundException, streamId, BucketId));
        }

        public OptimisticEventStream(ISnapshot snapshot, ICommitEvents persistence, int maxRevision)
            : this(snapshot.BucketId, snapshot.StreamId, persistence)
        {
            var commits = persistence.GetFrom(snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision,
                maxRevision);
            PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
            StreamRevision = snapshot.StreamRevision + _committed.Count;
        }

        public string BucketId { get; }
        public string StreamId { get; }
        public int StreamRevision { get; private set; }
        public int CommitSequence { get; private set; }
        public ICollection<EventMessage> CommittedEvents => _committedImmutableWrapper;
        public IDictionary<string, object> CommittedHeaders => _committedHeadersImmutableWrapper;
        public ICollection<EventMessage> UncommittedEvents => _eventsImmutableWraper;
        public IDictionary<string, object> UncommittedHeaders { get; } = new Dictionary<string, object>();

        public void Add(EventMessage uncommittedEvent)
        {
            if (uncommittedEvent == null) throw new ArgumentNullException(nameof(uncommittedEvent));

            if (uncommittedEvent.Body == null) throw new ArgumentException(nameof(uncommittedEvent.Body));

            Logger.LogTrace(Resources.AppendingUncommittedToStream, uncommittedEvent.Body.GetType(), StreamId,
                BucketId);
            _events.Add(uncommittedEvent);
        }

        public void CommitChanges(Guid commitId)
        {
            Logger.LogTrace(Resources.AttemptingToCommitChanges, StreamId, BucketId);

            if (_isPartialStream)
            {
                Logger.LogDebug(Resources.CannotAddCommitsToPartiallyLoadedStream, StreamId, BucketId, StreamRevision);

                RefreshStreamAfterConcurrencyException();

                throw new ConcurrencyException(string.Format(
                    Resources.CannotAddCommitsToPartiallyLoadedStream,
                    StreamId,
                    BucketId,
                    StreamRevision
                ));
            }

            if (_identifiers.Contains(commitId))
                throw new DuplicateCommitException(string.Format(Messages.DuplicateCommitIdException, StreamId,
                    BucketId, commitId));

            if (!HasChanges()) return;

            try
            {
                PersistChanges(commitId);
            }
            catch (ConcurrencyException cex)
            {
                Logger.LogDebug(Resources.UnderlyingStreamHasChanged, StreamId, BucketId, cex.Message);

                RefreshStreamAfterConcurrencyException();

                throw;
            }
        }

        public void ClearChanges()
        {
            Logger.LogTrace(Resources.ClearingUncommittedChanges, StreamId, BucketId);
            _events.Clear();
            UncommittedHeaders.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private void RefreshStreamAfterConcurrencyException()
        {
            var refreshFromRevision = StreamRevision + 1;
            var commits = _persistence.GetFrom(BucketId, StreamId, refreshFromRevision, int.MaxValue);
            PopulateStream(refreshFromRevision, int.MaxValue, commits);
        }

        private void PopulateStream(int minRevision, int maxRevision, IEnumerable<ICommit> commits)
        {
            _isPartialStream = false;
            foreach (var commit in commits ?? Enumerable.Empty<ICommit>())
            {
                _identifiers.Add(commit.CommitId);

                var currentRevision = commit.StreamRevision - commit.Events.Count + 1;
                // just in case the persistence returned more commits than it should be
                if (currentRevision > maxRevision)
                {
                    _isPartialStream = true;
                    Logger.LogDebug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    return;
                }

                Logger.LogTrace(Resources.AddingCommitsToStream, commit.CommitId, commit.Events.Count, StreamId,
                    BucketId);

                CommitSequence = commit.CommitSequence;

                CopyToCommittedHeaders(commit);
                CopyToEvents(minRevision, maxRevision, currentRevision, commit);
            }
        }

        private void CopyToCommittedHeaders(ICommit commit)
        {
            foreach (var key in commit.Headers.Keys) _committedHeaders[key] = commit.Headers[key];
        }

        private void CopyToEvents(int minRevision, int maxRevision, int currentRevision, ICommit commit)
        {
            foreach (var @event in commit.Events)
            {
                if (currentRevision > maxRevision)
                {
                    _isPartialStream = true;
                    Logger.LogDebug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    break;
                }

                if (currentRevision++ < minRevision)
                {
                    Logger.LogDebug(Resources.IgnoringBeforeRevision, commit.CommitId, StreamId, maxRevision);
                    continue;
                }

                _committed.Add(@event);
                StreamRevision = currentRevision - 1;
            }
        }

        private bool HasChanges()
        {
            if (_disposed) throw new ObjectDisposedException(Resources.AlreadyDisposed);

            if (_events.Count > 0) return true;

            Logger.LogInformation(Resources.NoChangesToCommit, StreamId, BucketId);
            return false;
        }

        private void PersistChanges(Guid commitId)
        {
            var attempt = BuildCommitAttempt(commitId);

            Logger.LogDebug(Resources.PersistingCommit, commitId, StreamId, BucketId, attempt.Events?.Count ?? 0);
            var commit = _persistence.Commit(attempt);

            PopulateStream(StreamRevision + 1, attempt.StreamRevision, new[] { commit });
            ClearChanges();
        }

        private CommitAttempt BuildCommitAttempt(Guid commitId)
        {
            Logger.LogTrace(Resources.BuildingCommitAttempt, commitId, StreamId, BucketId);
            return new CommitAttempt(
                BucketId,
                StreamId,
                StreamRevision + _events.Count,
                commitId,
                CommitSequence + 1,
                SystemTime.UtcNow,
                UncommittedHeaders.ToDictionary(x => x.Key, x => x.Value),
                _events.ToArray()); // check this for performance: preallocate the array size.
        }
    }
}