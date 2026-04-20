using NEventStore.Logging;
using NEventStore.Persistence;
using Microsoft.Extensions.Logging;

namespace NEventStore.PollingClient
{
    /// <summary>
    /// A Polling Client that uses the Asynchronous API of the store.
    /// </summary>
    public class AsyncPollingClient : IDisposable
    {
        /// <summary>
        /// Decorator used to enable Commit Re-Sequencing.
        /// During normal operations Commits might arrive out of order, or there can be holes in the sequence for whatever reason.
        /// In a rebuild scenario the stream is stable and commits will be read in the correct order, holes can be safely skipped.
        /// </summary>
        private sealed class CommitSequencer : IAsyncObserver<ICommit>
        {
            private readonly AsyncPollingClientObserver _observer;
            private readonly ILogger _logger = LogFactory.BuildLogger(typeof(CommitSequencer));
            /// <summary>
            /// How many retries we did on a hole.
            /// </summary>
            public int NumOfRetries { get; private set; }
            /// <summary>
            /// Max number of retries before skipping the hole.
            /// </summary>
            public int MaxNumOfRetriesBeforeSkip { get; set; } = 5;

            public CommitSequencer(AsyncPollingClientObserver observer, long checkpoint)
            {
                _observer = observer;
                _observer.ProcessedCheckpoint = checkpoint;
            }

            public Task OnCompletedAsync(CancellationToken cancellationToken = default)
            {
                return _observer.OnCompletedAsync(cancellationToken);
            }

            public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken = default)
            {
                return _observer.OnErrorAsync(ex, cancellationToken);
            }

            public Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken = default)
            {
                if (value.CheckpointToken != _observer.ProcessedCheckpoint + 1)
                {
                    if (NumOfRetries < MaxNumOfRetriesBeforeSkip)
                    {
                        NumOfRetries++;
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Hole detected on {Checkpoint} - {NumOfRetries}", value.CheckpointToken, NumOfRetries);
                        }
                        // stop the reading, the caller will wait then resume reading from the same checkpoint
                        return Task.FromResult(false);
                    }
                    // we reached the max number of retries, skip the hole
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Skipping hole on {Checkpoint}", value.CheckpointToken);
                    }
                }

                NumOfRetries = 0;
                return _observer.OnNextAsync(value, cancellationToken);
            }
        }

        /// <summary>
        /// Async Observer Decorator that wraps the original observer: it is used to:
        /// - Track the last processed commit.
        /// - Track if the user request to stop the polling from inside the observer.
        /// </summary>
        private sealed class AsyncPollingClientObserver : IAsyncObserver<ICommit>
        {
            private static readonly Task<bool> ContinuePollingTask = Task.FromResult(true);
            private static readonly Task<bool> StopPollingTask = Task.FromResult(false);
            private readonly IAsyncObserver<ICommit> _observer;

            public bool StopPolling { get; private set; }
            /// <summary>
            /// The last correctly processed commit.
            /// </summary>
            public long ProcessedCheckpoint { get; internal set; }

            public AsyncPollingClientObserver(IAsyncObserver<ICommit> observer)
            {
                _observer = observer;
            }
            public Task OnCompletedAsync(CancellationToken cancellationToken = default)
            {
                return _observer.OnCompletedAsync(cancellationToken);
            }

            public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken = default)
            {
                return _observer.OnErrorAsync(ex, cancellationToken);
            }

#if NET8_0_OR_GREATER
            public Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken = default)
            {
                StopPolling = false;
                var goOnTask = _observer.OnNextAsync(value, cancellationToken);

                // The common catch-up path often uses observers that complete synchronously after
                // recording the commit. Modern targets can detect that completed task and update
                // checkpoint/stop state without allocating an async state machine per commit.
                // Faulted or incomplete tasks intentionally flow through the awaited path so
                // exception and cancellation behavior stays identical to the compatibility target.
                if (goOnTask.IsCompletedSuccessfully)
                {
                    return ToCompletedTask(CompleteOnNext(value, goOnTask.Result));
                }

                return CompleteOnNextAsync(value, goOnTask);
            }
#else
            public async Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken = default)
            {
                StopPolling = false;
                var goOn = await _observer.OnNextAsync(value, cancellationToken).ConfigureAwait(false);
                return CompleteOnNext(value, goOn);
            }
#endif

            private bool CompleteOnNext(ICommit value, bool goOn)
            {
                StopPolling = !goOn;
                ProcessedCheckpoint = value.CheckpointToken;
                return goOn;
            }

            private async Task<bool> CompleteOnNextAsync(ICommit value, Task<bool> goOnTask)
            {
                return CompleteOnNext(value, await goOnTask.ConfigureAwait(false));
            }

            private static Task<bool> ToCompletedTask(bool goOn)
            {
                return goOn ? ContinuePollingTask : StopPollingTask;
            }
        }

        private readonly ILogger _logger;
        private readonly IPersistStreams _persistStreams;
        private readonly AsyncPollingClientObserver _commitObserver;
        private readonly Int32 _waitInterval;
        private readonly int _holeDetectionWaitInterval;
        private Func<long, CancellationToken, Task>? _pollingFunc;
        private Int64 _checkpointToken;
        private CancellationTokenSource? _cancellationTokeSource;
        private Task? _pollingTask;
        private int _isPolling;
        private CommitSequencer? _commitSequencer;

        /// <summary>
        /// Creates an NEventStore Polling Client
        /// </summary>
        /// <param name="persistStreams">the store to check</param>
        /// <param name="commitObserver">the observer to notify</param>
        /// <param name="waitInterval">Interval in Milliseconds to wait when the provider
        /// return no more commit and the next request</param>
        /// <param name="holeDetectionWaitInterval">Interval in Milliseconds to wait before retry when a hole was detected in the stream. 0 = disable hole detection.</param>
        public AsyncPollingClient(
            IPersistStreams persistStreams,
            IAsyncObserver<ICommit> commitObserver,
            Int32 waitInterval = 100,
            Int32 holeDetectionWaitInterval = 0)
        {
            if (commitObserver is null)
            {
                throw new ArgumentNullException(nameof(commitObserver));
            }

            _persistStreams = persistStreams ?? throw new ArgumentNullException(nameof(persistStreams));
            _commitObserver = new AsyncPollingClientObserver(commitObserver);
            _logger = LogFactory.BuildLogger(GetType());
            _waitInterval = waitInterval;
            _holeDetectionWaitInterval = holeDetectionWaitInterval;
            LastActivityTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Tells the caller the last tick count when the last activity occurred. This is useful for the caller
        /// to setup Health check that verify if the polling client is really active and it is really loading new commits.
        /// This value is obtained with DateTime.UtcNow
        /// </summary>
        public DateTime LastActivityTimestamp { get; private set; }

        /// <summary>
        /// If the polling client encounter an exception it immediately retry, but we need to tell to the caller code
        /// that the last polling encounter an error. This is needed to detect a polling client stuck as an example
        /// with deserialization problems.
        /// </summary>
        public String? LastPollingError { get; private set; }

        /// <summary>
        /// Start the polling client.
        /// </summary>
        public void Start(Int64 checkpointToken = 0)
        {
            _cancellationTokeSource = new CancellationTokenSource();
            ConfigurePollingClient(checkpointToken);
            StartPollingThread(_cancellationTokeSource.Token);
        }

        /// <summary>
        /// Start the polling client.
        /// </summary>
        public void Start(string bucketId, Int64 checkpointToken = 0)
        {
            _cancellationTokeSource = new CancellationTokenSource();
            ConfigurePollingClient(checkpointToken, bucketId);
            StartPollingThread(_cancellationTokeSource.Token);
        }

        /// <summary>
        /// Configure the polling function to get commits from the store.
        /// </summary>
        internal void ConfigurePollingClient(Int64 checkpointToken = 0, string? bucketId = null)
        {
            _checkpointToken = checkpointToken;
            IAsyncObserver<ICommit> commitObserver;
            if (_holeDetectionWaitInterval > 0)
            {
                _commitSequencer = new CommitSequencer(_commitObserver, _checkpointToken);
                commitObserver = _commitSequencer;
            }
            else
            {
                commitObserver = _commitObserver;
            }
            if (bucketId == null)
                _pollingFunc = (long checkpointToken, CancellationToken cancellationToken) => _persistStreams.GetFromAsync(checkpointToken, commitObserver, cancellationToken);
            else
                _pollingFunc = (long checkpointToken, CancellationToken cancellationToken) => _persistStreams.GetFromAsync(bucketId, checkpointToken, commitObserver, cancellationToken);
        }

        /// <summary>
        /// Simply start the timer that will queue wake up tokens.
        /// </summary>
        private void StartPollingThread(CancellationToken cancellationToken)
        {
            if (_pollingFunc == null) return;
            // Keep a direct handle to the worker task so StopAsync/Dispose can await the actual
            // polling loop instead of repeatedly sleeping and hoping the worker has observed the
            // cancellation request. That makes shutdown bounded by the in-flight poll itself,
            // which is the real lifetime contract callers care about.
            _pollingTask = Task.Run(() => PollingLoopAsync(cancellationToken), CancellationToken.None);
        }

        private async Task PollingLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && !_commitObserver.StopPolling)
                {
                    // Poll again immediately after consuming commits so catch-up paths can drain the
                    // store as fast as the observer allows. The idle delay is only useful when the
                    // previous poll did not advance the checkpoint and we genuinely need to back off.
                    bool madeProgress = await PollAsyncInternal(cancellationToken).ConfigureAwait(false);
                    if (!madeProgress && !cancellationToken.IsCancellationRequested && !_commitObserver.StopPolling)
                    {
                        await Task.Delay(_waitInterval, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancellation is the normal stop path. Swallow it here so StopAsync can await the
                // worker task directly without turning routine shutdown into an error.
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Error during Polling operation");
                }
                LastPollingError = ex.Message;
            }
        }

        /// <summary>
        /// Stop the polling client.
        /// </summary>
        public async Task StopAsync()
        {
            var cancellationTokenSource = _cancellationTokeSource;
            var pollingTask = _pollingTask;

            cancellationTokenSource?.Cancel();
            _cancellationTokeSource = null;
            _pollingTask = null;

            if (pollingTask == null)
            {
                return;
            }

            try
            {
                await pollingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The polling loop swallows cooperative cancellation, but keep this catch to avoid
                // leaking a cancellation fault if a future refactor changes where the exception is observed.
            }
        }

        /// <summary>
        /// Poll the store for new commits.
        /// </summary>
        public Task PollAsync(CancellationToken token)
        {
            // The public manual-poll API does not expose whether the checkpoint advanced, but
            // the polling loop needs that signal internally. Returning the Task<bool> as Task
            // preserves the public contract while avoiding an extra async wrapper allocation.
            return PollAsyncInternal(token);
        }

        private async Task<bool> PollAsyncInternal(CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
            {
                try
                {
                    LastActivityTimestamp = DateTime.UtcNow;
                    bool madeProgress = false;
                    if (_commitSequencer == null)
                    {
                        long checkpointBeforePoll = _checkpointToken;
                        await _pollingFunc!(_checkpointToken, token).ConfigureAwait(false);
                        _checkpointToken = _commitObserver.ProcessedCheckpoint;
                        madeProgress = _checkpointToken != checkpointBeforePoll;
                    }
                    else
                    {
                        do
                        {
                            long checkpointBeforePoll = _checkpointToken;
                            await _pollingFunc!(_checkpointToken, token).ConfigureAwait(false);
                            _checkpointToken = _commitObserver.ProcessedCheckpoint;
                            madeProgress |= _checkpointToken != checkpointBeforePoll;
                            if (_commitObserver.StopPolling)
                            {
                                break;
                            }
                            // todo: should also check if the user requested to stop the polling from the observer ?
                            if (_commitSequencer.NumOfRetries > 0)
                            {
                                await Task.Delay(_holeDetectionWaitInterval, token).ConfigureAwait(false);
                            }
                        }
                        while (!token.IsCancellationRequested && _commitSequencer.NumOfRetries > 0);
                    }

                    return madeProgress;
                }
                finally
                {
                    Interlocked.Exchange(ref _isPolling, 0);
                }
            }

            return false;
        }

        private Boolean _isDisposed;

        /// <summary>
        /// Dispose the polling client.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the polling client.
        /// </summary>
        protected virtual void Dispose(Boolean isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }
            if (isDisposing)
            {
                StopAsync().GetAwaiter().GetResult();
            }
            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~AsyncPollingClient()
        {
            Dispose(false);
        }
    }
}
