namespace NEventStore.PollingClient
{
    using System;
    using System.Reactive.Subjects;
    using NEventStore.Persistence;

    /// <summary>
    /// Represents a client that poll the storage for latest commits.
    /// </summary>
    public sealed class PollingClientRx
    {
        private readonly PollingClient2 _pollingClient2;

        private readonly Subject<ICommit> _subject;

        public PollingClientRx(
            IPersistStreams persistStreams,
            Int32 waitInterval = 5000)
        {
            if (persistStreams is null)
            {
                throw new ArgumentNullException(nameof(persistStreams));
            }
            if (waitInterval <= 0)
            {
                throw new ArgumentException("Must be greater than 0", nameof(waitInterval));
            }
            _subject = new Subject<ICommit>();
            _pollingClient2 = new PollingClient2(persistStreams, c =>
            {
                _subject.OnNext(c);
                return PollingClient2.HandlingResult.MoveToNext;
            },
            waitInterval: waitInterval);
        }

        public IDisposable Subscribe(IObserver<ICommit> observer)
        {
            return _subject.Subscribe(observer);
        }

        private Int64 _checkpointToObserveFrom;

        public IObservable<ICommit> ObserveFrom(Int64 checkpointToken = 0)
        {
            _checkpointToObserveFrom = checkpointToken;
            return _subject;
        }

        internal void Start()
        {
            _pollingClient2.StartFrom(_checkpointToObserveFrom);
        }

        internal void Dispose()
        {
            _pollingClient2.Dispose();
        }

        internal void StartFromBucket(string bucketId)
        {
            _pollingClient2.StartFromBucket(bucketId, 0);
        }
    }
}