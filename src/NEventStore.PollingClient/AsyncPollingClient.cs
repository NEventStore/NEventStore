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
        private class CommitSequencer : IAsyncObserver<ICommit>
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

            public Task OnCompletedAsync(CancellationToken cancellationToken)
            {
                return _observer.OnCompletedAsync(cancellationToken);
            }

            public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken)
            {
                return _observer.OnErrorAsync(ex, cancellationToken);
            }

            public Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken)
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
        private class AsyncPollingClientObserver : IAsyncObserver<ICommit>
        {
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
            public Task OnCompletedAsync(CancellationToken cancellationToken)
            {
                return _observer.OnCompletedAsync(cancellationToken);
            }

            public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken)
            {
                return _observer.OnErrorAsync(ex, cancellationToken);
            }

            public async Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken)
            {
                StopPolling = false;
                var goOn = await _observer.OnNextAsync(value, cancellationToken).ConfigureAwait(false);
                StopPolling = !goOn;
                ProcessedCheckpoint = value.CheckpointToken;
                return goOn;
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
        private bool _stopped = true;
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
            _stopped = false;
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested && !_commitObserver.StopPolling)
                    {
                        await PollAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(_waitInterval, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    _stopped = true;
                }
            }, cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception.Flatten().InnerException;
                        _logger.LogError($"Error during Poll, first exception: {ex.Message}.\n{ex}");
                        LastPollingError = ex.Message;
                    }
                }, cancellationToken);
        }

        /// <summary>
        /// Stop the polling client.
        /// </summary>
        public async Task StopAsync()
        {
            _cancellationTokeSource?.Cancel();
            _cancellationTokeSource = null;

            while (!_stopped)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        public async Task PollAsync(CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
            {
                try
                {
                    LastActivityTimestamp = DateTime.UtcNow;
                    if (_commitSequencer == null)
                    {
                        await _pollingFunc!(_checkpointToken, token).ConfigureAwait(false);
                        _checkpointToken = _commitObserver.ProcessedCheckpoint;
                    }
                    else
                    {
                        do
                        {
                            await _pollingFunc!(_checkpointToken, token).ConfigureAwait(false);
                            _checkpointToken = _commitObserver.ProcessedCheckpoint;
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
                }
                finally
                {
                    Interlocked.Exchange(ref _isPolling, 0);
                }
            }
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
                StopAsync().Wait();
            }
            _isDisposed = true;
        }

        ~AsyncPollingClient()
        {
            Dispose(false);
        }
    }
}
