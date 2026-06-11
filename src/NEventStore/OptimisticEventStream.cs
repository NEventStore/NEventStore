using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        // This stream mostly appends events, counts them, and copies them into arrays when building commit
        // attempts. A contiguous List<T> matches that access pattern better than a linked list because it
        // avoids one allocation per node and makes the eventual CopyTo path cheaper. The public contract
        // still stays ICollection<EventMessage> through the immutable wrappers, so callers do not observe
        // the storage swap.
        private readonly List<EventMessage> _committed = [];
        private readonly ImmutableCollection<EventMessage> _committedImmutableWrapper;
        private readonly Dictionary<string, object> _committedHeaders = [];
        private readonly ImmutableDictionary<string, object> _committedHeadersImmutableWrapper;
        private readonly List<EventMessage> _events = [];
        private readonly ImmutableCollection<EventMessage> _eventsImmutableWrapper;
        // Duplicate commit identifiers are enforced at the persistence boundary rather than inside
        // this stream instance. A stream can be reopened from any revision range, so the commits
        // loaded into this object are not guaranteed to be the complete history for the stream.
        // Keeping a local cache of loaded CommitIds would therefore only reject the subset of
        // duplicates that happened to be observed by this instance and would miss duplicates from
        // earlier unseen commits. The persistence engine is the only component that can validate
        // the full BucketId + StreamId + CommitId identity across reopened streams and concurrent
        // writers, so the stream delegates that invariant there.
        private readonly ICommitEvents _persistence;
        private readonly ICommitEventsAsync _persistenceAsync;
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
        /// Create a new instance of the OptimisticEventStream class.
        /// </summary>
        public OptimisticEventStream(string bucketId, string streamId, ICommitEvents persistence, ICommitEventsAsync persistenceAsync)
        {
            if (string.IsNullOrWhiteSpace(bucketId))
            {
                throw new ArgumentException($"'{nameof(bucketId)}' cannot be null or whitespace.", nameof(bucketId));
            }

            if (string.IsNullOrWhiteSpace(streamId))
            {
                throw new ArgumentException($"'{nameof(streamId)}' cannot be null or whitespace.", nameof(streamId));
            }

            BucketId = bucketId;
            StreamId = streamId;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _persistenceAsync = persistenceAsync ?? throw new ArgumentNullException(nameof(persistenceAsync));
            _committedImmutableWrapper = new ImmutableCollection<EventMessage>(_committed);
            _eventsImmutableWrapper = new ImmutableCollection<EventMessage>(_events);
            _committedHeadersImmutableWrapper = new ImmutableDictionary<string, object>(_committedHeaders);
        }

        private void EnsureStreamIsNew()
        {
            if (_committed.Count > 0 || _events.Count > 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot call Initialize on a stream already used: Bucked: {0}, Stream: {1}, Committed Events: {2}, New Events: {3}", BucketId, StreamId, _committed.Count, _events.Count));
            }
        }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="StreamNotFoundException"></exception>
        public void Initialize(int minRevision, int maxRevision)
        {
            EnsureStreamIsNew();
            IEnumerable<ICommit> commits = _persistence.GetFrom(BucketId, StreamId, minRevision, maxRevision);
            PopulateStream(minRevision, maxRevision, commits);

            if (minRevision > 0 && _committed.Count == 0)
            {
                throw new StreamNotFoundException(String.Format(CultureInfo.InvariantCulture, Messages.StreamNotFoundException, StreamId, BucketId));
            }
        }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="StreamNotFoundException"></exception>
        public async Task InitializeAsync(int minRevision, int maxRevision, CancellationToken cancellationToken)
        {
            EnsureStreamIsNew();
            _isPartialStream = false;
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (commit, _) =>
                {
                    InnerPopulateStream(minRevision, maxRevision, commit);
                    return Task.FromResult(true);
                });
            await _persistenceAsync.GetFromAsync(BucketId, StreamId, minRevision, maxRevision, observer, cancellationToken).ConfigureAwait(false);

            if (minRevision > 0 && _committed.Count == 0)
            {
                throw new StreamNotFoundException(String.Format(CultureInfo.InvariantCulture, Messages.StreamNotFoundException, StreamId, BucketId));
            }
        }

        private void EnsureSnapshotIsForThisStream(ISnapshot snapshot)
        {
            if (BucketId != snapshot.BucketId || StreamId != snapshot.StreamId)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The snapshot is for a different stream. Stream BucketId: {0}, StreamId: {1}; Snapshot BucketId: {2}, StreamId: {3}", BucketId, StreamId, snapshot.BucketId, snapshot.StreamId));
            }
        }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Initialize(ISnapshot snapshot, int maxRevision)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            EnsureSnapshotIsForThisStream(snapshot);
            EnsureStreamIsNew();
            IEnumerable<ICommit> commits = _persistence.GetFrom(snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision, maxRevision);
            PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
            StreamRevision = snapshot.StreamRevision + _committed.Count;
        }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStream class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task InitializeAsync(ISnapshot snapshot, int maxRevision, CancellationToken cancellationToken)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
            EnsureSnapshotIsForThisStream(snapshot);
            EnsureStreamIsNew();
            int minRevision = snapshot.StreamRevision + 1;
            _isPartialStream = false;
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (commit, _) =>
                {
                    InnerPopulateStream(minRevision, maxRevision, commit);
                    return Task.FromResult(true);
                });
            await _persistenceAsync.GetFromAsync(snapshot.BucketId, snapshot.StreamId, snapshot.StreamRevision, maxRevision, observer, cancellationToken).ConfigureAwait(false);
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
        public ICommit? CommitChanges(Guid commitId)
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
                    CultureInfo.InvariantCulture,
                    Resources.CannotAddCommitsToPartiallyLoadedStream,
                    StreamId,
                    BucketId,
                    StreamRevision
                    ));
            }

            if (!HasChanges())
            {
                return null;
            }

            try
            {
                return PersistChanges(commitId);
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

        /// <inheritdoc/>
        public async Task<ICommit?> CommitChangesAsync(Guid commitId, CancellationToken cancellationToken)
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

                await RefreshStreamAfterConcurrencyExceptionAsync(cancellationToken).ConfigureAwait(false);

                throw new ConcurrencyException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CannotAddCommitsToPartiallyLoadedStream,
                    StreamId,
                    BucketId,
                    StreamRevision
                    ));
            }

            if (!HasChanges())
            {
                return null;
            }

            try
            {
                return await PersistChangesAsync(commitId, cancellationToken).ConfigureAwait(false);
            }
            catch (ConcurrencyException cex)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug(Resources.UnderlyingStreamHasChanged, StreamId, BucketId, cex.Message);
                }

                await RefreshStreamAfterConcurrencyExceptionAsync(cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        private void RefreshStreamAfterConcurrencyException()
        {
            int refreshFromRevision = StreamRevision + 1;
            IEnumerable<ICommit> commits = _persistence.GetFrom(BucketId, StreamId, refreshFromRevision, int.MaxValue);
            PopulateStream(refreshFromRevision, int.MaxValue, commits);
        }

        private Task RefreshStreamAfterConcurrencyExceptionAsync(CancellationToken cancellationToken)
        {
            int refreshFromRevision = StreamRevision + 1;
            const int maxRevision = int.MaxValue;
            _isPartialStream = false;
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (commit, _) =>
                {
                    InnerPopulateStream(refreshFromRevision, maxRevision, commit);
                    return Task.FromResult(true);
                });
            return _persistenceAsync.GetFromAsync(BucketId, StreamId, refreshFromRevision, maxRevision, observer, cancellationToken);
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

        private void PopulateStream(int minRevision, int maxRevision, ICommit commit)
        {
            _isPartialStream = false;
            InnerPopulateStream(minRevision, maxRevision, commit);
        }

        private void PopulateStream(int minRevision, int maxRevision, IEnumerable<ICommit> commits)
        {
            _isPartialStream = false;
            PreSizeCommittedEventsBuffer(commits, minRevision, maxRevision);
            foreach (var commit in commits ?? [])
            {
                InnerPopulateStream(minRevision, maxRevision, commit);
            }
        }

        private void InnerPopulateStream(int minRevision, int maxRevision, ICommit commit)
        {
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
            EnsureCommittedCapacityForRange(currentRevision, commit.Events.Count, minRevision, maxRevision);
            CopyToEvents(minRevision, maxRevision, currentRevision, commit);
        }

        private void CopyToCommittedHeaders(ICommit commit)
        {
            foreach (var header in commit.Headers)
            {
                _committedHeaders[header.Key] = header.Value;
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

        private void PreSizeCommittedEventsBuffer(IEnumerable<ICommit> commits, int minRevision, int maxRevision)
        {
            // We only pre-size when the persistence result is already an in-memory collection that can be
            // safely enumerated twice. That keeps the optimization local to known collections such as the
            // in-memory engine and test stubs without forcing deferred providers into an unexpected second
            // read just to estimate capacity.
            if (commits is IReadOnlyCollection<ICommit> readOnlyCommits)
            {
                EnsureCommittedCapacity(readOnlyCommits, minRevision, maxRevision);
                return;
            }

            if (commits is ICollection<ICommit> commitsCollection)
            {
                EnsureCommittedCapacity(commitsCollection, minRevision, maxRevision);
            }
        }

        private void EnsureCommittedCapacity(IEnumerable<ICommit> commits, int minRevision, int maxRevision)
        {
            var additionalEventCount = 0;
            foreach (var commit in commits)
            {
                var firstEventRevision = commit.StreamRevision - commit.Events.Count + 1;
                additionalEventCount += CountEventsInRequestedRange(firstEventRevision, commit.Events.Count, minRevision, maxRevision);
            }

            if (additionalEventCount > 0)
            {
                SetCommittedCapacityAtLeast(_committed.Count + additionalEventCount);
            }
        }

        private void EnsureCommittedCapacityForRange(int firstEventRevision, int eventCount, int minRevision, int maxRevision)
        {
            var additionalEventCount = CountEventsInRequestedRange(firstEventRevision, eventCount, minRevision, maxRevision);
            if (additionalEventCount > 0)
            {
                SetCommittedCapacityAtLeast(_committed.Count + additionalEventCount);
            }
        }

        private void SetCommittedCapacityAtLeast(int requiredCapacity)
        {
            // Capacity is used instead of List<T>.EnsureCapacity so the optimization still compiles on the
            // older target frameworks supported by NEventStore. The growth strategy stays geometric rather
            // than setting Capacity to the exact requested size because the async read path materializes one
            // commit at a time. Exact growth would force a full-array copy on nearly every append there and
            // turn stream opens back into an O(n^2) allocation pattern for long streams.
            if (_committed.Capacity >= requiredCapacity)
            {
                return;
            }

            var newCapacity = _committed.Capacity == 0 ? 4 : _committed.Capacity;
            while (newCapacity < requiredCapacity)
            {
                var doubledCapacity = newCapacity * 2;
                if (doubledCapacity <= 0)
                {
                    newCapacity = requiredCapacity;
                    break;
                }

                newCapacity = doubledCapacity;
            }

            _committed.Capacity = newCapacity;
        }

        private static int CountEventsInRequestedRange(int firstEventRevision, int eventCount, int minRevision, int maxRevision)
        {
            if (eventCount == 0)
            {
                return 0;
            }

            var lastEventRevision = firstEventRevision + eventCount - 1;
            var firstIncludedRevision = Math.Max(firstEventRevision, minRevision);
            var lastIncludedRevision = Math.Min(lastEventRevision, maxRevision);
            return firstIncludedRevision > lastIncludedRevision
                ? 0
                : lastIncludedRevision - firstIncludedRevision + 1;
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

        private ICommit? PersistChanges(Guid commitId)
        {
            CommitAttempt attempt = BuildCommitAttempt(commitId);

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.PersistingCommit, commitId, StreamId, BucketId, attempt.Events?.Count ?? 0);
            }
            var commit = _persistence.Commit(attempt);
            if (commit != null)
            {
                PopulateStream(StreamRevision + 1, attempt.StreamRevision, commit);
                ClearChanges();
            }
            return commit;
        }

        private async Task<ICommit?> PersistChangesAsync(Guid commitId, CancellationToken cancellationToken)
        {
            CommitAttempt attempt = BuildCommitAttempt(commitId);

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.PersistingCommit, commitId, StreamId, BucketId, attempt.Events?.Count ?? 0);
            }
            var commit = await _persistenceAsync.CommitAsync(attempt, cancellationToken).ConfigureAwait(false);
            if (commit != null)
            {
                PopulateStream(StreamRevision + 1, attempt.StreamRevision, commit);
                ClearChanges();
            }
            return commit;
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
                CopyUncommittedHeaders(),
                CopyUncommittedEvents());
        }

        private Dictionary<string, object> CopyUncommittedHeaders()
        {
            var headers = new Dictionary<string, object>(UncommittedHeaders.Count);
            foreach (var header in UncommittedHeaders)
            {
                headers[header.Key] = header.Value;
            }

            return headers;
        }

        private EventMessage[] CopyUncommittedEvents()
        {
            var events = new EventMessage[_events.Count];
            _events.CopyTo(events, 0);
            return events;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
        }
    }
}
