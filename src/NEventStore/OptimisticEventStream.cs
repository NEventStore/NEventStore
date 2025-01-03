using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore
{
    /// <summary>
    /// Represents a stream of events that can be committed and appended to.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This behaves like a stream--not a .NET 'Stream' object, but a stream nonetheless.")]
    public sealed class OptimisticEventStream : IEventStream
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticEventStream));
        private readonly ICollection<EventMessage> _committed = new LinkedList<EventMessage>();
        private readonly ImmutableCollection<EventMessage> _committedImmutableWrapper;
        private readonly Dictionary<string, object> _committedHeaders = new Dictionary<string, object>();
        private readonly ImmutableDictionary<string, object> _committedHeadersImmutableWrapper;
        private readonly ICollection<EventMessage> _events = new LinkedList<EventMessage>();
        private readonly ImmutableCollection<EventMessage> _eventsImmutableWrapper;
        private readonly ICollection<Guid> _identifiers = new HashSet<Guid>();
        private readonly ICommitEvents _persistence;
        private bool _disposed;
        // a stream is considered partial if we haven't read all the events in a commit
        private bool _isPartialStream;

        /// <inheritdoc/>
        public string BucketId { get; }
        /// <inheritdoc/>
        public string StreamId { get; }
        /// <inheritdoc/>
        public int StreamRevision { get; private set; }
        /// <inheritdoc/>
        public int CommitSequence { get; private set; }
        /// <inheritdoc/>
        public ICollection<EventMessage> CommittedEvents { get => _committedImmutableWrapper; }
        /// <inheritdoc/>
        public IDictionary<string, object> CommittedHeaders { get => _committedHeadersImmutableWrapper; }
        /// <inheritdoc/>
        public ICollection<EventMessage> UncommittedEvents { get => _eventsImmutableWrapper; }
        /// <inheritdoc/>
        public IDictionary<string, object> UncommittedHeaders { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence)
        {
            BucketId = bucketId;
            StreamId = streamId;
            _persistence = persistence;
            _committedImmutableWrapper = new ImmutableCollection<EventMessage>(_committed);
            _eventsImmutableWrapper = new ImmutableCollection<EventMessage>(_events);
            _committedHeadersImmutableWrapper = new ImmutableDictionary<string, object>(_committedHeaders);
        }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        /// <exception cref="StreamNotFoundException"></exception>
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

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        public OptimisticEventStream(ISnapshot snapshot, ICommitEvents persistence, int maxRevision)
            : this(snapshot.BucketId, snapshot.StreamId, persistence)
        {
            IEnumerable<ICommit> commits = persistence.GetFrom(snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision, maxRevision);
            PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
            StreamRevision = snapshot.StreamRevision + _committed.Count;
        }

        /// <inheritdoc/>
        public void Add(EventMessage uncommittedEvent)
        {
            if (uncommittedEvent == null)
            {
                throw new ArgumentNullException(nameof(uncommittedEvent));
            }

            if (uncommittedEvent.Body == null)
            {
                throw new ArgumentException(nameof(uncommittedEvent.Body));
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.AppendingUncommittedToStream, uncommittedEvent.Body.GetType(), StreamId, BucketId);
            }
            _events.Add(uncommittedEvent);
        }

        /// <inheritdoc/>
        public void CommitChanges(Guid commitId)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.AttemptingToCommitChanges, StreamId, BucketId);
            }

            if (_isPartialStream)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug(Resources.CannotAddCommitsToPartiallyLoadedStream, StreamId, BucketId, StreamRevision);
                }

                RefreshStreamAfterConcurrencyException();

                throw new ConcurrencyException(string.Format(
                    Resources.CannotAddCommitsToPartiallyLoadedStream,
                    StreamId,
                    BucketId,
                    StreamRevision
                    ));
            }

            if (_identifiers.Contains(commitId))
            {
                throw new DuplicateCommitException(String.Format(Messages.DuplicateCommitIdException, StreamId, BucketId, commitId));
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
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug(Resources.UnderlyingStreamHasChanged, StreamId, BucketId, cex.Message);
                }

                RefreshStreamAfterConcurrencyException();

                throw;
            }
        }

        private void RefreshStreamAfterConcurrencyException()
        {
            int refreshFromRevision = StreamRevision + 1;
            IEnumerable<ICommit> commits = _persistence.GetFrom(BucketId, StreamId, refreshFromRevision, int.MaxValue);
            PopulateStream(refreshFromRevision, int.MaxValue, commits);
        }

        /// <inheritdoc/>
        public void ClearChanges()
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.ClearingUncommittedChanges, StreamId, BucketId);
            }
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
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    }
                    return;
                }

                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(Resources.AddingCommitsToStream, commit.CommitId, commit.Events.Count, StreamId, BucketId);
                }

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
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    }
                    break;
                }

                if (currentRevision++ < minRevision)
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug(Resources.IgnoringBeforeRevision, commit.CommitId, StreamId, maxRevision);
                    }
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

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.NoChangesToCommit, StreamId, BucketId);
            }
            return false;
        }

        private void PersistChanges(Guid commitId)
        {
            CommitAttempt attempt = BuildCommitAttempt(commitId);

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.PersistingCommit, commitId, StreamId, BucketId, attempt.Events?.Count ?? 0);
            }
            ICommit commit = _persistence.Commit(attempt);

            PopulateStream(StreamRevision + 1, attempt.StreamRevision, new[] { commit });
            ClearChanges();
        }

        private CommitAttempt BuildCommitAttempt(Guid commitId)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.BuildingCommitAttempt, commitId, StreamId, BucketId);
            }
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

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
        }
    }
}