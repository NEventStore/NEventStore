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

        public override IObserveCommits ObserveFrom(ICheckpoint checkpoint)
        {
            return new PollingObserveCommits(PersistStreams, checkpoint, _interval);
        }

        public override IObserveCommits ObserveFromBegininng()
        {
            throw new NotImplementedException();
        }

        private class PollingObserveCommits : IObserveCommits
        {
            private readonly IPersistStreams _persistStreams;
            private ICheckpoint _checkpoint;
            private readonly int _interval;
            private readonly Subject<ICommit> _subject = new Subject<ICommit>();
            private readonly CancellationTokenSource _stopRequested = new CancellationTokenSource();
            private TaskCompletionSource<Unit> _runningTaskCompletionSource;

            public PollingObserveCommits(IPersistStreams persistStreams, ICheckpoint checkpoint, int interval)
            {
                _persistStreams = persistStreams;
                _checkpoint = checkpoint;
                _interval = interval;
            }

            public IDisposable Subscribe(IObserver<ICommit> observer)
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
                TaskHelpers.Delay(_interval, _stopRequested.Token).WhenCompleted(_ =>
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
                IEnumerable<ICommit> commits = _persistStreams.GetFrom(_checkpoint);
                foreach (var commit in commits)
                {
                    if (commit.Checkpoint.CompareTo(_checkpoint) < 0)
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