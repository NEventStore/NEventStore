namespace NEventStore.Client
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using NEventStore.Persistence;

    public sealed class PollingClient : ClientBase
    {
        private readonly int _interval;

        public PollingClient(IPersistStreams persistStreams, int interval)
            : base(persistStreams)
        {
            _interval = interval;
        }

        public override IObserveCommits ObserveFrom(int checkpoint)
        {
            return new PollingObserveCommits(PersistStreams, checkpoint, _interval);
        }

        private class PollingObserveCommits : IObserveCommits
        {
            private readonly IPersistStreams _persistStreams;
            private int _checkpoint;
            private readonly int _interval;
            private readonly Subject<Commit> _subject = new Subject<Commit>();
            private readonly CancellationTokenSource _stopRequested = new CancellationTokenSource();
            private TaskCompletionSource<Unit> _runningTaskCompletionSource;

            public PollingObserveCommits(IPersistStreams persistStreams, int checkpoint, int interval)
            {
                _persistStreams = persistStreams;
                _checkpoint = checkpoint;
                _interval = interval;
            }

            public IDisposable Subscribe(IObserver<Commit> observer)
            {
                return _subject.Subscribe(observer);
            }

            public void Dispose()
            {
                _stopRequested.Cancel();
                _subject.Dispose();
                _runningTaskCompletionSource.TrySetResult(new Unit());
            }

            public Task Start()
            {
                if (_runningTaskCompletionSource != null)
                {
                    return _runningTaskCompletionSource.Task;
                }
                _runningTaskCompletionSource = new TaskCompletionSource<Unit>();
                Poll();
                return _runningTaskCompletionSource.Task;
            }

            private void Poll()
            {
                if (_stopRequested.IsCancellationRequested)
                {
                    Dispose();
                    return;
                }
                TaskHelpers.Delay(_interval, _stopRequested.Token)
                    .WhenCompleted(
                    _ =>
                        {
                            try
                            {
                                GetNextCommits(_stopRequested.Token);
                            }
                            catch (Exception ex)
                            {
                                _subject.OnError(ex);
                                _runningTaskCompletionSource.TrySetException(ex);
                                return;
                            }
                            Poll();
                        },
                     _ => Dispose());
            }

            private void GetNextCommits(CancellationToken cancellationToken)
            {
                IEnumerable<Commit> commits = _persistStreams.GetSince(_checkpoint);
                foreach (var commit in commits)
                {
                    if (commit.Checkpoint < _checkpoint)
                    {
                        continue;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _subject.OnCompleted();
                        return;
                    }
                    _subject.OnNext(commit);
                    _checkpoint = commit.Checkpoint;
                }
            }
        }

        
    }
}