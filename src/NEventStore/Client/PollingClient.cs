namespace NEventStore.Client
{
    using System;
    using System.Collections.Generic;
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
            private bool _started;
            private readonly CancellationTokenSource _stopRequested = new CancellationTokenSource();

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
            }

            public void Start()
            {
                if (_started)
                {
                    return;
                }
                Poll();
                _started = true;
            }

            private async void Poll()
            {
                try
                {
                    GetNextCommits();
                    while (!_stopRequested.IsCancellationRequested)
                    {
                        await Task.Delay(_interval, _stopRequested.Token);
                        GetNextCommits();
                    }
                }
                catch (Exception ex)
                {
                    _subject.OnError(ex);
                    return;
                }
                _subject.OnCompleted();
            }

            private void GetNextCommits()
            {
                IEnumerable<Commit> commits = _persistStreams.GetSince(_checkpoint);
                foreach (var commit in commits)
                {
                    if (commit.Checkpoint < _checkpoint)
                    {
                        continue;
                    }
                    _subject.OnNext(commit);
                    _checkpoint = commit.Checkpoint;
                }
            }
        }
    }
}