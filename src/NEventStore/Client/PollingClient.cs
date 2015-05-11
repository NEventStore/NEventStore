namespace NEventStore.Client
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    /// <summary>
    /// Represents a client that poll the storage for latest commits.
    /// </summary>
    public sealed class PollingClient : ClientBase
    {
        private readonly int _interval;

        public PollingClient(IPersistStreams persistStreams, int interval = 5000) : base(persistStreams)
        {
            if (persistStreams == null)
            {
                throw new ArgumentNullException("persistStreams");
            }
            if (interval <= 0)
            {
                throw new ArgumentException(Messages.MustBeGreaterThanZero.FormatWith("interval"));
            }
            _interval = interval;
        }

        /// <summary>
        /// Observe commits from the sepecified checkpoint token. If the token is null,
        /// all commits from the beginning will be observed.
        /// </summary>
        /// <param name="checkpointToken">The checkpoint token.</param>
        /// <returns>
        /// An <see cref="IObserveCommits" /> instance.
        /// </returns>
        public override IObserveCommits ObserveFrom(string checkpointToken = null)
        {
            return new PollingObserveCommits(PersistStreams, _interval, null, checkpointToken);
        }

        public override IObserveCommits ObserveFromBucket(string bucketId, string checkpointToken = null)
        {
            return new PollingObserveCommits(PersistStreams, _interval, bucketId, checkpointToken);
        }

        private class PollingObserveCommits : IObserveCommits
        {
            private ILog Logger = LogFactory.BuildLogger(typeof (PollingClient));
            private readonly IPersistStreams _persistStreams;
            private string _checkpointToken;
            private readonly int _interval;
            private readonly string _bucketId;
            private readonly Subject<ICommit> _subject = new Subject<ICommit>();
            private readonly CancellationTokenSource _stopRequested = new CancellationTokenSource();
            private TaskCompletionSource<Unit> _runningTaskCompletionSource;
            private int _isPolling = 0;

            public PollingObserveCommits(IPersistStreams persistStreams, int interval, string bucketId, string checkpointToken = null)
            {
                _persistStreams = persistStreams;
                _checkpointToken = checkpointToken;
                _interval = interval;
                _bucketId = bucketId;
            }

            public IDisposable Subscribe(IObserver<ICommit> observer)
            {
                return _subject.Subscribe(observer);
            }

            public void Dispose()
            {
                _stopRequested.Cancel();
                _subject.Dispose();
                if (_runningTaskCompletionSource != null)
                {
                    _runningTaskCompletionSource.TrySetResult(new Unit());
                }
            }

            public Task Start()
            {
                if (_runningTaskCompletionSource != null)
                {
                    return _runningTaskCompletionSource.Task;
                }
                _runningTaskCompletionSource = new TaskCompletionSource<Unit>();
                PollLoop();
                return _runningTaskCompletionSource.Task;
            }

            public void PollNow()
            {
                DoPoll();
            }

            private void PollLoop()
            {
                if (_stopRequested.IsCancellationRequested)
                {
                    Dispose();
                    return;
                }
                TaskHelpers.Delay(_interval, _stopRequested.Token)
                    .WhenCompleted(_ =>
                    {
                        DoPoll();
                        PollLoop();
                    },_ => Dispose());
            }

            private void DoPoll()
            {
                if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
                {
                    try
                    {
                        var commits = _bucketId == null ? 
                            _persistStreams.GetFrom(_checkpointToken) :
                            _persistStreams.GetFrom(_bucketId, _checkpointToken);

                        foreach (var commit in commits)
                        {
                            if (_stopRequested.IsCancellationRequested)
                            {
                                _subject.OnCompleted();
                                return;
                            }
                            _subject.OnNext(commit);
                            _checkpointToken = commit.CheckpointToken;
                        }
                    }
                    catch (Exception ex)
                    {
                        // These exceptions are expected to be transient
                        Logger.Error(ex.ToString());
                    }
                    Interlocked.Exchange(ref _isPolling, 0);
                }
            }
        }
    }
}